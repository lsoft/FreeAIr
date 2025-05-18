grammar PromptMarkdown;

markdownFile
   : paragraph+ EOF
   ;

paragraph
   : code_block
   | not_a_code_block
   | newline
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
   : H1
   | H2
   | H3
   | H4
   | H5
   | H6
   ;

sentence
  : (xml_block | code_line | url | freeair_command | freeair_solution_item | word | whitespace)+
  ;

whitespace
   : WHITESPACE
   ;

xml_block
   : XML_BLOCK
   ;

freeair_command
   : FREEAIR_COMMAND
   ;

freeair_solution_item
   : FREEAIR_SOLUTION_ITEM
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

H1
   : '#' WHITESPACE WORD+
   ;

H2
   : '##' WHITESPACE WORD+
   ;

H3
   : '###' WHITESPACE WORD+
   ;

H4
   : '####' WHITESPACE WORD+
   ;

H5
   : '#####' WHITESPACE WORD+
   ;

H6
   : '######' WHITESPACE WORD+
   ;

XML_BLOCK
   : XML_HEAD WORD XML_TAIL
   ;

XML_HEAD
   : '<' WORD '>'
   ;

XML_TAIL
   : '</' WORD '>'
   ;

CODE_LINE
   : '`' (~[\r\n`])+ '`'
   ;

URL
   : '[' WORD ']' '(' WORD ')'
   ;

CODE_BLOCK
   : '```' (~[`])+ '```'
   ;

FREEAIR_COMMAND
   : '/' WORD
   ;

FREEAIR_SOLUTION_ITEM
   : '#' WORD
   ;

WORD
  : NOT_A_WHITESPACE+
  ;

NOT_A_WHITESPACE
  : ~[\t \r\n\u000C]
  ;

WHITESPACE
  : [\t \u000C]
  ;

NEWLINE
   : '\r\n'
   ;
