

: bmpbase a000 ;
: colbase 8c00 ;

:asm hires 
bb lda,# d011 sta, # enable
15 lda,# dd00 sta, # vic bank 2
38 lda,# d018 sta,
56 lda,# 1 sta, # no basic
;asm

:asm lores
9b lda,# d011 sta,
17 lda,# dd00 sta,
17 lda,# d018 sta,
;asm

: clrcol ( fgbgcol -- )
colbase 3e8 fill
0 bmpbase 1f40 fill ;

: blkcol ( x y c -- )
rot 8 / rot 8 / 28 *
+ colbase + c! ;

create mask
80 c, 40 c, 20 c, 10 c,
8 c, 4 c, 2 c, 1 c,

var penx var peny
0 penx ! 0 peny !

: blitop 0 ;

: blitloc ( x y -- mask addr )
dup fff8 and 28 *
swap 7 and + swap # y x
dup 7 and ['] mask + c@ # y x bit
-rot # bit y x
fff8 and + bmpbase + ;

: plot ( x y -- )
2dup peny ! penx !
2dup c8 >= swap 140 >= or
if 2drop exit then
blitloc swap over c@
[ here @ to blitop ] or swap c! ;

: peek ( x y -- b )
blitloc c@ and ;

: dx 0 ; : dy 0 ;
: sx 0 ; : sy 0 ;
var err

: line ( x y -- )
2dup peny @ - abs to dy
penx @ - abs to dx
2dup
peny @ swap s< if 1 else ffff then to sy
penx @ swap s< if 1 else ffff then to sx
dx dy - err !
dy negate to dy

begin
 err @ 2* dup
 dy s> if
  dy err +!
  sx penx +! 
 then
 dx s< if
  dx err +!
  sy peny +!
 then
 penx @ peny @ plot
 2dup peny @ = swap penx @ = and if
  2drop exit
 then
again ;

: cx 0 ;
: cy 0 ;

: plot4 ( x y -- x y )
over cx + over cy + plot
over if # x?
over cx swap - over cy + plot
then
dup if # y?
over cx + over cy swap - plot
then
over not not over not not and if
over cx swap - over cy swap - plot
then ;

: plot8 ( x y -- x y )
plot4
2dup <> if
swap plot4 swap
then ;

: circle ( cx cy r -- )
dup negate err !
swap to cy
swap to cx
0 # x y
begin 2dup s< not while
plot8
dup err +!
1+
dup err +!
err @ 0< not if
over negate err +!
swap 1- swap
over negate err +!
then
repeat 2drop ;

: erase if
['] xor else
['] or then blitop ! ;

# paul heckbert seed fill
# from graphics gems
var stk
: spush ( y xl xr dy -- )
# y out of bounds?
3 pick over + dup 0< swap c7 > or if
2drop 2drop exit then
stk @ tuck c! 1+ # dy
tuck ! 2+ # xr
tuck ! 2+ # xl
tuck c! 1+ # y
stk ! ;

: x1 0 ; : x2 0 ;

: spop ( -- y xl )
stk @ 1- dup c@ swap # y
1- 1- dup @ to x1
1- 1- dup @ to x2
1- dup c@
ff = if ffff else 1 then to dy
stk ! ;

var l

: flood ( x y -- )
2dup c8 >= swap 140 >= or
if 2drop exit then
2dup peek if 2drop exit then

here @ stk !
# push y x x 1
2dup swap dup 1 spush
# push y+1 x x -1
1+ swap dup ffff spush

begin here @ stk @ < while
spop # y
dy +

# left line
x1 over # y x y
begin
2dup peek not # y x y !peek
2 pick 0< not and while
2dup plot
swap 1- swap repeat
over x1 # y x y x x1
s< not if
branch [ here @ >r 0 , ] # goto skip
then
# y x y ...
over 1+ dup l ! 
# y x y l
x1 < if # l < x1?
# y x y
dup l @ # y x y y l
x1 1- # y x y y l x1-1
dy negate spush
then
# y x y
nip x1 1+ swap # x=x1+1

begin
# y x y
begin 2dup peek not
2 pick 140 < and while
2dup plot swap 1+ swap
repeat
# y x y
dup l @
# y x y y l
3 pick 1-
# y x y y l x-1
dy spush
# y x y

# leak on right?
over x2 1+ > if
dup # y x y y
x2 1+ # y x y y x2+1
3 pick 1- # y x y y x2+1 x-1
dy negate spush
then

# skip:
# y x y
[ r> here @ over - swap ! ]

swap 1+ swap
begin
2dup peek not not
# y x2 x y peek
2 pick x2 <= and while
swap 1+ swap repeat

over l ! # l=x

# y x y
over x2 > until

2drop drop
repeat ; 

# test flood
( hires 5 clrcol

0 90 plot 13e 90 line
2 0 plot 2 c7 line
5 0 plot 5 c7 line
3 30 flood

60 60 20 circle
60 60 10 circle
7f 60 flood

10 10 plot
20 10 line
20 35 line
10 35 line
10 12 line
8 8 plot
25 8 line
25 40 line
8 40 line
8 18 line
18 24 flood lores )
