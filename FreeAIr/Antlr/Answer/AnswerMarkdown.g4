grammar AnswerMarkdown;

markdownFile
  : block+ EOF
  ;

block
  : paragraph
  | horizontal_rule
  | blockquote
  ;

paragraph
  : code_block
  | not_a_code_block
  | newline
  ;

blockquote
  : BLOCKQUOTE_START (sentence | newline)*
  ;
  
horizontal_rule
  : HORIZONTAL_RULE
  ;

newline
  : NEWLINE
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
  : '#' WHITESPACE+ WORD (WHITESPACE+ WORD)* NEWLINE?
  ;
h2
  : '##' WHITESPACE+ WORD (WHITESPACE+ WORD)* NEWLINE?
  ;
h3
  : '###' WHITESPACE+ WORD (WHITESPACE+ WORD)* NEWLINE?
  ;
h4
  : '####' WHITESPACE+ WORD (WHITESPACE+ WORD)* NEWLINE?
  ;
h5
  : '#####' WHITESPACE+ WORD (WHITESPACE+ WORD)* NEWLINE?
  ;
h6
  : '######' WHITESPACE+ WORD (WHITESPACE+ WORD)* NEWLINE?
  ;

sentence
  : (xml_block | code_line | quick_link | url | image | word | whitespace)+ NEWLINE?
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
  : WORD
  ;


// Lexer rules

HORIZONTAL_RULE
  : [ \t\r\n]* '-' ([ \t\r\n]* '-')+ ([ \t\r\n]* '-')+ [ \t\r\n]* NEWLINE
  | [ \t\r\n]* '*' ([ \t\r\n]* '*')+ ([ \t\r\n]* '*')+ [ \t\r\n]* NEWLINE
  | [ \t\r\n]* '_' ([ \t\r\n]* '_')+ ([ \t\r\n]* '_')+ [ \t\r\n]* NEWLINE
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
  : '<' XML_NODE_NAME '>' ~[<>]* '</' XML_NODE_NAME '>'
  ;

CODE_LINE
  : '`' (~[\r\n`])+ '`'
  ;

CODE_BLOCK
  : '```' (~[`])+ '```'
  ;

URL
  : '[' ~[\]\r\n]+ ']'      '('      ~[)\r\n ]+        ([\t \u000C]+ '"' ~["]+ '"')?     ')'
  ;

BLOCKQUOTE_START
  : '> '      // обязателен пробел после '>'
  ;

WORD
  : NOT_A_WHITESPACE_SYMBOL+
  ;

XML_NODE_NAME
  : SYMBOL_FOR_XML_NODE+
  ;

NOT_A_WHITESPACE_SYMBOL
  : ~[\t \r\n\u000C]
  ;

SYMBOL_FOR_XML_NODE
  : ~[\\@/:.\t \r\n\u000C]
  ;

WHITESPACE
  : [\t \u000C]
  ;

NEWLINE
  : '\r'? '\n'
  ;
