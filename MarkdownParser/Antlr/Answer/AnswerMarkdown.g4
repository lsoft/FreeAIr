grammar AnswerMarkdown;

markdownFile
  : (block newline)+ EOF
  ;

block
  : paragraph
  | horizontal_rule
  | blockquote
  ;

paragraph
  : code_block
  | not_a_code_block
  ;

blockquote
  : BLOCKQUOTE_START sentence*
  ;
  
horizontal_rule
  : HORIZONTAL_RULE
  ;

newline
  : NEWLINE_N
  ;

code_block
  : CODE_BLOCK
  ;

not_a_code_block
  : heading
  | sentence
  ;

heading
  : h6
  | h5
  | h4
  | h3
  | h2
  | h1
  ;

h1
  : HEADER1_START WHITESPACE+ WORD (WHITESPACE+ WORD)* NEWLINE_N?
  ;
h2
  : HEADER2_START WHITESPACE+ WORD (WHITESPACE+ WORD)* NEWLINE_N?
  ;
h3
  : HEADER3_START WHITESPACE+ WORD (WHITESPACE+ WORD)* NEWLINE_N?
  ;
h4
  : HEADER4_START WHITESPACE+ WORD (WHITESPACE+ WORD)* NEWLINE_N?
  ;
h5
  : HEADER5_START WHITESPACE+ WORD (WHITESPACE+ WORD)* NEWLINE_N?
  ;
h6
  : HEADER6_START WHITESPACE+ WORD (WHITESPACE+ WORD)* NEWLINE_N?
  ;

sentence
  : (xml_block | code_line | quick_link | url | image | word | punctuation | whitespace)+
  ;

image
  : IMAGE
  ;

quick_link
  : QUICK_LINK
  ;

whitespace
  : WHITESPACE+
  ;

xml_block
  : XML_BLOCK
  ;

code_line
  : CODE_LINE
  ;

url
  : URL
  ;

word
  : WORD_SYMBOL+
  ;

punctuation
  : PUNCTUATION
  ;


// Lexer rules

NEWLINE_N
  : '\n'
  ;

NEWLINE_R
  : '\r' -> skip
  ;

HORIZONTAL_RULE
  : [ \t\r\n]* '-' ([ \t\r\n]* '-')+ ([ \t\r\n]* '-')+ [ \t\r\n]* NEWLINE_N
  | [ \t\r\n]* '*' ([ \t\r\n]* '*')+ ([ \t\r\n]* '*')+ [ \t\r\n]* NEWLINE_N
  | [ \t\r\n]* '_' ([ \t\r\n]* '_')+ ([ \t\r\n]* '_')+ [ \t\r\n]* NEWLINE_N
  ;

IMAGE
  : '!' '[' ~[\]\r\n]+ ']' '(' ~[)\r\n ]+ ([\t \u000C]+ '"' ~["]+ '"')? ')'
  ;

QUICK_LINK
  : '<'
    ( ~[>\n] )*                                         // 0 или более символов, кроме '>' и '\n'
    ( ( '@' | '.' | '/' | '\\' | ':' ) ( ~[>\n] )* )+   // обязателен хотя бы один из этих символов
    '>'
  ;

XML_BLOCK
  : '<' [\p{L}\p{N}_]+ '>' .*? '</' [\p{L}\p{N}_]+ '>'
  ;
//интересный способ остановки { InputStream.LA(1) == '<' && InputStream.LA(2) == '/' }?

CODE_LINE
  : '`' (~[\r\n`])+ '`'
  ;

CODE_BLOCK
  : '```' (~[`])+ '```'
  ;

URL
  : '[' ~[\]\r\n]+ ']'      '('      ~[)\r\n ]+        ([\t \u000C]+ '"' ~["]+ '"')?     ')'
  ;

HEADER1_START
  : {Column == 0}? '# '      // обязателен пробел после '#'
  ;
HEADER2_START
  : {Column == 0}? '## '      // обязателен пробел после '#'
  ;
HEADER3_START
  : {Column == 0}? '### '      // обязателен пробел после '#'
  ;
HEADER4_START
  : {Column == 0}? '#### '      // обязателен пробел после '#'
  ;
HEADER5_START
  : {Column == 0}? '##### '      // обязателен пробел после '#'
  ;
HEADER6_START
  : {Column == 0}? '###### '      // обязателен пробел после '#'
  ;

BLOCKQUOTE_START
  : {Column == 0}? '> '      // обязателен пробел после '>'
  ;

WORD_SYMBOL
  : [\p{L}\p{N}_]
  ;

PUNCTUATION
    : ~[\p{L}\p{N}_\t \u000C]
    ;

WHITESPACE
  : [\p{Z}]
  ;
