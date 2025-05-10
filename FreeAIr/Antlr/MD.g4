grammar MD;

markdownFile
   : (code_block | line)+ EOF
   ;

code_block
   : CODE_BLOCK
   ;

line
   : paragraph
   | heading
   | TEXT_NEWLINE
   ;

heading
   : H1
   | H2
   | H3
   | H4
   | H5
   | H6
   ;

paragraph
   : sentence+
   ;

sentence
   : code_line
   | freeair_command
   | freeair_solution_item
   | url
   | TEXT
   ;

url
   : URL
   ;

code_line
   : CODE_LINE
   ;

freeair_command
   : FREEAIR_COMMAND
   ;

freeair_solution_item
   : FREEAIR_SOLUTION_ITEM
   ;

// Lexer rules

H1
   : '#' WHITESPACE TEXT
   ;

H2
   : '##' WHITESPACE TEXT
   ;

H3
   : '###' WHITESPACE TEXT
   ;

H4
   : '####' WHITESPACE TEXT
   ;

H5
   : '#####' WHITESPACE TEXT
   ;

H6
   : '######' WHITESPACE TEXT
   ;

CODE_LINE
   : '`' (~[\r\n`])+ '`'
   ;

TEXT_NEWLINE
   : '\r\n'
   ;

TEXT
   : (~[\r\n#`<>/[])+
   ;

URL
   : '[' TEXT ']' '(' TEXT ')'
   ;

CODE_BLOCK
   : '```' (~[`])+ '```'
   ;

FREEAIR_COMMAND
   : '/' (~[\t/ \r\n\u000C])+
   ;

FREEAIR_SOLUTION_ITEM
   : '#' (~[\t/ \r\n\u000C])+
   ;

WHITESPACE
   : ( '\t' | ' ' | '\r' | '\n' | '\u000C' )
   ;
