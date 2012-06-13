

# examples from c64 step by step
# programming, gfx book 3, phil cornes

s" rnd" load
s" sin" load

: lineweb
hires 7 clrcol
5 begin
dup 140 < while
dup 0 plot 96 c8 line
dup c7 plot 96 0 line
a + repeat drop ;

: rndline
hires 10 clrcol
80 begin ?dup while
rnd ab / 20 -
rnd f8 / 20 - line
1- repeat ;

: radiant
hires d0 clrcol
100 begin
?dup while
32 64 plot
dup cos 12c 2* d* drop 32 12c - +
over sin 12c 2* d* drop 64 12c - +
line
1- repeat ;

: diamond
hires 12 clrcol
2 d020 c!
0 64 plot
0 begin
dup c8 < while
a0 over line
13f 64 line
a0 over c7 swap - line
0 64 line
5 + repeat ;

: reccircgo ( x r -- )
dup if
2dup 64 swap circle
2dup + over 2/ recurse
2dup - over 2/ recurse
then 2drop ;

: reccirc
hires 7 clrcol
a0 50 reccircgo ;

var yd

: 2reccircgo ( x r -- )
dup if
2dup yd @ swap circle
2dup c7 yd @ - swap circle
d yd +!
2dup + over 2/ recurse
2dup - over 2/ recurse
d negate yd +!
then 2drop ;

: 2reccirc
hires 7 clrcol
64 yd !
a0 50 2reccircgo ;

: erasecirc
hires 7 clrcol
0 begin dup 140 < while
dup 0 plot dup c7 line
14 + repeat drop
0 begin dup c7 < while
dup 0 swap plot dup 13f swap line
14 + repeat drop
;
