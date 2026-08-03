using FreeAIr.Grep;
using System.Text;
using Xunit;

namespace FreeAIr.Search.Tests
{
    /// <summary>
    /// The matching behind the SearchFileContent tool: what a pattern finds, what it reports and
    /// where it stops.
    /// </summary>
    public sealed class GrepFacts
    {
        private const string _text = """
            using System;

            namespace Billing
            {
                public sealed class Invoice
                {
                    public void Pay()
                    {
                        //the invoice is paid here
                    }
                }
            }
            """;

        [Fact]
        public void A_plain_text_is_found_with_its_line_number()
        {
            var result = Search(new GrepOptions { Pattern = "class Invoice" });

            var match = Assert.Single(result.Matches);
            Assert.Equal("Billing\\Invoice.cs", match.RelativePath);
            Assert.Equal(5, match.LineNumber);
            Assert.Equal("    public sealed class Invoice", match.Line);
            Assert.Equal(1, result.FilesScanned);
            Assert.Equal(1, result.FilesWithMatches);
            Assert.False(result.LimitReached);
        }

        [Fact]
        public void A_plain_text_is_a_text_and_not_a_pattern()
        {
            //the model asks for what it sees in the code; a dot in it is a dot
            var text = "a.b\naxb";

            var result = Search(new GrepOptions { Pattern = "a.b" }, text);

            Assert.Equal("a.b", Assert.Single(result.Matches).Line);
        }

        [Fact]
        public void The_case_is_ignored_unless_it_was_asked_about()
        {
            Assert.Single(Search(new GrepOptions { Pattern = "class INVOICE" }).Matches);

            Assert.Empty(
                Search(new GrepOptions { Pattern = "class INVOICE", CaseSensitive = true }).Matches
                );
        }

        [Fact]
        public void A_whole_word_search_does_not_match_a_longer_name()
        {
            var text = "var chat = new ChatWindow();";

            Assert.Single(Search(new GrepOptions { Pattern = "Chat" }, text).Matches);

            var whole = Search(
                new GrepOptions
                {
                    Pattern = "Chat",
                    WholeWord = true,
                    CaseSensitive = true
                },
                text
                );

            Assert.Empty(whole.Matches);
        }

        [Fact]
        public void A_regular_expression_is_one_when_it_was_asked_to_be()
        {
            var result = Search(
                new GrepOptions
                {
                    Pattern = "public\\s+(sealed\\s+)?class\\s+\\w+",
                    UseRegularExpression = true
                }
                );

            Assert.Equal(5, Assert.Single(result.Matches).LineNumber);
        }

        [Fact]
        public void A_broken_regular_expression_is_refused_before_anything_is_read()
        {
            //the tool turns this into a failed call with the reason in it, so that the model can
            //write the next pattern correctly. RegexParseException on net8 and a plain
            //ArgumentException on the net48 the VSIX runs on - the catch has to hold for both
            Assert.ThrowsAny<ArgumentException>(
                () => new GrepEngine(
                    new GrepOptions
                    {
                        Pattern = "class (Invoice",
                        UseRegularExpression = true
                    }
                    )
                );
        }

        [Fact]
        public void An_inverted_search_returns_the_lines_which_do_not_match()
        {
            var text = "one\ntwo\nthree";

            var result = Search(
                new GrepOptions
                {
                    Pattern = "o",
                    InvertMatch = true
                },
                text
                );

            var match = Assert.Single(result.Matches);
            Assert.Equal("three", match.Line);
            Assert.Equal(3, match.LineNumber);
            Assert.Equal(1, result.FilesWithMatches);
        }

        [Fact]
        public void An_inverted_search_which_finds_nothing_means_every_line_matched()
        {
            var result = Search(
                new GrepOptions
                {
                    Pattern = "e",
                    InvertMatch = true
                },
                "one\ntwo\nthree"
                );

            //`two` has no `e` in it, so this is about the ordinary case being intact
            Assert.Equal(new[] { "two" }, result.Matches.Select(m => m.Line));
        }

        [Fact]
        public void An_inverted_search_carries_the_context_and_the_limit_of_an_ordinary_one()
        {
            var engine = new GrepEngine(
                new GrepOptions
                {
                    Pattern = "keep",
                    InvertMatch = true,
                    ContextLineCount = 1,
                    MaxMatchCount = 2
                }
                );

            var result = engine.CreateResult();

            Assert.False(
                engine.SearchIn("A.cs", "keep\ndrop 1\ndrop 2\ndrop 3", result)
                );

            Assert.Equal(2, result.Matches.Count);
            Assert.True(result.LimitReached);
            Assert.Equal(new[] { "keep" }, result.Matches[0].ContextBefore);
        }

        [Fact]
        public void Every_line_ending_counts_the_same()
        {
            foreach (var ending in new[] { "\r\n", "\n", "\r" })
            {
                var text = string.Join(ending, "one", "two", "target", "four");

                var result = Search(new GrepOptions { Pattern = "target" }, text);

                Assert.Equal(3, Assert.Single(result.Matches).LineNumber);
            }
        }

        [Fact]
        public void A_line_is_reported_once_however_many_times_it_matches()
        {
            var result = Search(new GrepOptions { Pattern = "a" }, "aaa\nb");

            Assert.Single(result.Matches);
        }

        [Fact]
        public void The_lines_around_a_match_come_with_it_when_they_were_asked_for()
        {
            var result = Search(
                new GrepOptions
                {
                    Pattern = "the invoice is paid",
                    ContextLineCount = 2
                }
                );

            var match = Assert.Single(result.Matches);
            Assert.Equal(new[] { "        public void Pay()", "        {" }, match.ContextBefore);
            Assert.Equal(new[] { "        }", "    }" }, match.ContextAfter);
        }

        [Fact]
        public void The_context_stops_at_the_edges_of_the_file()
        {
            var result = Search(
                new GrepOptions
                {
                    Pattern = "using System",
                    ContextLineCount = 3
                }
                );

            var match = Assert.Single(result.Matches);
            Assert.Empty(match.ContextBefore);
            Assert.Equal(3, match.ContextAfter.Count);
        }

        [Fact]
        public void No_context_was_asked_for_and_none_is_carried()
        {
            var match = Assert.Single(Search(new GrepOptions { Pattern = "class Invoice" }).Matches);

            Assert.Empty(match.ContextBefore);
            Assert.Empty(match.ContextAfter);
        }

        [Fact]
        public void The_limit_stops_the_search_and_says_so()
        {
            //a truncated answer which does not admit to being one is read by the model as the whole
            //truth, which is worse than no answer
            var engine = new GrepEngine(
                new GrepOptions
                {
                    Pattern = "line",
                    MaxMatchCount = 3
                }
                );

            var result = engine.CreateResult();

            var text = string.Join("\n", Enumerable.Range(0, 10).Select(i => $"line {i}"));

            var wantsMore = engine.SearchIn("A.cs", text, result);
            Assert.False(wantsMore);

            //and the next file is not even looked at
            Assert.False(engine.SearchIn("B.cs", text, result));

            Assert.Equal(3, result.Matches.Count);
            Assert.True(result.LimitReached);
            Assert.Equal(1, result.FilesScanned);
        }

        [Fact]
        public void A_very_long_line_is_cut_around_the_match_rather_than_at_its_head()
        {
            //generated and minified files are single lines of a hundred kilobytes, and the head of
            //such a line never holds what was searched for
            var line = new string('x', 5000) + "NEEDLE" + new string('y', 5000);

            var result = Search(
                new GrepOptions
                {
                    Pattern = "NEEDLE",
                    MaxLineLength = 120
                },
                line
                );

            var reported = Assert.Single(result.Matches).Line;

            Assert.Contains("NEEDLE", reported);
            Assert.True(reported.Length <= 120 + 6);
            Assert.StartsWith("...", reported);
            Assert.EndsWith("...", reported);
        }

        [Fact]
        public void A_pattern_which_cannot_be_finished_in_time_loses_its_file_and_nothing_else()
        {
            var engine = new GrepEngine(
                new GrepOptions
                {
                    Pattern = "(a+)+$",
                    UseRegularExpression = true,
                    RegexTimeout = TimeSpan.FromMilliseconds(50)
                }
                );

            var result = engine.CreateResult();

            engine.SearchIn("Bad.cs", new string('a', 40) + "!", result);
            engine.SearchIn("Good.cs", "aaa", result);

            Assert.Equal(1, result.TimedOutFileCount);
            Assert.Single(result.Matches);
            Assert.Equal("Good.cs", result.Matches[0].RelativePath);
        }

        private static GrepSearchResult Search(
            GrepOptions options,
            string? text = null
            )
        {
            var engine = new GrepEngine(options);
            var result = engine.CreateResult();
            engine.SearchIn("Billing\\Invoice.cs", text ?? _text, result);
            return result;
        }
    }

    /// <summary>
    /// The file filter of the search: the masks a model writes, and the files which are not text.
    /// </summary>
    public sealed class GrepFileFilterFacts
    {
        [Fact]
        public void An_unspecified_mask_lets_everything_through()
        {
            var mask = FileMask.Parse(null);

            Assert.True(mask.MatchesEverything);
            Assert.True(mask.IsMatch("Whatever\\Thing.bin"));
        }

        [Theory]
        [InlineData("*.cs", "Billing\\Invoice.cs", true)]
        [InlineData("*.cs", "Billing\\Invoice.xaml", false)]
        [InlineData("*.cs;*.xaml", "Ui\\Window.xaml", true)]
        [InlineData("Invoice.cs", "Billing\\Invoice.cs", true)]
        [InlineData("invoice.CS", "Billing\\Invoice.cs", true)]
        [InlineData("Invoice.??", "Billing\\Invoice.cs", true)]
        public void A_mask_without_a_folder_in_it_is_about_the_file_name(
            string masks,
            string relativePath,
            bool expected
            )
        {
            Assert.Equal(expected, FileMask.Parse(masks).IsMatch(relativePath));
        }

        [Theory]
        [InlineData("Rag\\*.cs", "Rag\\Find\\RagShortlist.cs", true)]
        [InlineData("Rag/*.cs", "Rag\\Find\\RagShortlist.cs", true)]
        [InlineData("Rag\\*.cs", "FreeAIr\\Find\\RagShortlist.cs", false)]
        [InlineData("src\\*.cs", "a\\b\\src\\Thing.cs", true)]
        public void A_mask_with_a_folder_in_it_is_about_the_path(
            string masks,
            string relativePath,
            bool expected
            )
        {
            Assert.Equal(expected, FileMask.Parse(masks).IsMatch(relativePath));
        }

        [Theory]
        [InlineData("*.cs;!*.Designer.cs", "Ui\\Window.cs", true)]
        [InlineData("*.cs;!*.Designer.cs", "Ui\\Window.Designer.cs", false)]
        [InlineData("*.cs;!*.designer.CS", "Ui\\Window.Designer.cs", false)]
        [InlineData("*.cs;!*.Designer.cs;!*.g.cs", "Ui\\Window.g.cs", false)]
        [InlineData("*.cs;! *.g.cs", "Ui\\Window.g.cs", false)]
        [InlineData("*.cs;!Generated\\*.cs", "Generated\\Window.cs", false)]
        [InlineData("*.cs;!Generated\\*.cs", "Ui\\Window.cs", true)]
        public void An_exclusion_wins_over_the_mask_which_let_the_file_in(
            string masks,
            string relativePath,
            bool expected
            )
        {
            //generated code is what a search over a solution drowns in, and the budget is spent on
            //it long before the answer is reached
            Assert.Equal(expected, FileMask.Parse(masks).IsMatch(relativePath));
        }

        [Fact]
        public void A_filter_made_of_exclusions_alone_means_everything_else()
        {
            var mask = FileMask.Parse("!*.Designer.cs");

            Assert.False(mask.MatchesEverything);
            Assert.True(mask.IsMatch("Ui\\Window.cs"));
            Assert.True(mask.IsMatch("Readme.md"));
            Assert.False(mask.IsMatch("Ui\\Window.Designer.cs"));
        }

        [Fact]
        public void An_exclamation_mark_with_nothing_after_it_is_not_a_mask()
        {
            var mask = FileMask.Parse("!");

            Assert.True(mask.MatchesEverything);
            Assert.True(mask.IsMatch("Ui\\Window.cs"));
        }

        [Fact]
        public void The_extensions_which_never_hold_text_are_known_by_name()
        {
            Assert.True(TextFile.HasBinaryExtension("a\\b\\FreeAIr.dll"));
            Assert.True(TextFile.HasBinaryExtension("Icon.PNG"));
            Assert.False(TextFile.HasBinaryExtension("Program.cs"));
            Assert.False(TextFile.HasBinaryExtension("Makefile"));
        }

        [Fact]
        public void A_file_with_a_zero_byte_in_it_is_not_searched()
        {
            var bytes = Encoding.ASCII.GetBytes("MZ\0\0this is an executable");

            Assert.False(TextFile.TryDecode(bytes, out _));
        }

        [Fact]
        public void A_byte_order_mark_is_believed_and_not_returned()
        {
            var utf8 = new byte[] { 0xEF, 0xBB, 0xBF }
                .Concat(Encoding.UTF8.GetBytes("привет"))
                .ToArray();

            Assert.True(TextFile.TryDecode(utf8, out var text));
            Assert.Equal("привет", text);

            var utf16 = Encoding.Unicode.GetPreamble()
                .Concat(Encoding.Unicode.GetBytes("hello"))
                .ToArray();

            Assert.True(TextFile.TryDecode(utf16, out var wide));
            Assert.Equal("hello", wide);
        }

        [Fact]
        public void A_file_in_an_older_codepage_still_matches_on_its_ascii()
        {
            //a single byte codepage is not UTF-8 and cannot be decoded as one; refusing the file
            //would lose every identifier in it, which is what the search is usually about
            var bytes = Encoding.ASCII.GetBytes("public class Счёт")
                .Take("public class ".Length)
                .Concat(new byte[] { 0xD1, 0xF7, 0xB8, 0xF2 })
                .ToArray();

            Assert.True(TextFile.TryDecode(bytes, out var text));
            Assert.StartsWith("public class ", text);
        }

        [Fact]
        public void An_empty_file_is_text_with_nothing_in_it()
        {
            Assert.True(TextFile.TryDecode(Array.Empty<byte>(), out var text));
            Assert.Equal(string.Empty, text);
            Assert.Empty(TextFile.SplitLines(text));
        }
    }

    /// <summary>
    /// The walk over the solution folder, which is what the `all_files` scope searches.
    /// </summary>
    public sealed class FileWalkerFacts
    {
        [Fact]
        public void The_folders_which_hold_build_output_and_history_are_not_part_of_the_source_tree()
        {
            //without this the search spends its whole budget inside bin, obj and .git, and answers
            //with the copies instead of the sources
            var root = Path.Combine(Path.GetTempPath(), "freeair-grep-" + Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(Path.Combine(root, "Billing"));
                Directory.CreateDirectory(Path.Combine(root, "bin", "Release"));
                Directory.CreateDirectory(Path.Combine(root, ".git"));
                Directory.CreateDirectory(Path.Combine(root, "node_modules", "left-pad"));

                File.WriteAllText(Path.Combine(root, "FreeAIr.sln"), "solution");
                File.WriteAllText(Path.Combine(root, "Billing", "Invoice.cs"), "class Invoice {}");
                File.WriteAllText(Path.Combine(root, "bin", "Release", "Invoice.cs"), "class Invoice {}");
                File.WriteAllText(Path.Combine(root, ".git", "HEAD"), "ref: refs/heads/main");
                File.WriteAllText(Path.Combine(root, "node_modules", "left-pad", "index.js"), "module.exports");

                var found = FileWalker.EnumerateFiles(root, default)
                    .Select(f => f.Substring(root.Length + 1))
                    .OrderBy(f => f)
                    .ToArray();

                Assert.Equal(
                    new[] { "Billing\\Invoice.cs", "FreeAIr.sln" },
                    found
                    );
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
