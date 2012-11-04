  d value CR
: clrscr e544 jsr ;

: assert 0= if
begin 1 d020 +! again then ;

6001 value bufstart
0 bufstart 1- c! # reverse sentinel

var eof ( ram eof )
0 eof !

var homepos ( position at screen home )
var curlinestart

( cursor screen pos )
var curx
var cury
0 value need-refresh
0 value need-refresh-line
0 value insert-active

10 allot dup
value filename-len
1+ value filename

: editpos
curlinestart @ curx @ + ;

:asmsub foundeol
zptmp ldy, 0 sty,x
zptmp 1+ ldy, 1 sty,x
;asm

:asm print-line
0 ldy,x zptmp sty,
1 ldy,x zptmp 1+ sty,
0 ldy,#
here @
zptmp lda,(y)
e716 jsr, # putchar
zptmp inc,
2 bne,
zptmp 1+ inc,
0 cmp,#
foundeol -branch beq,
d cmp,#
foundeol -branch beq,
jmp,

:asm next-line
0 ldy,x zptmp sty,
1 ldy,x zptmp 1+ sty,
0 ldy,#
here @
zptmp lda,(y)
zptmp inc, 2 bne, zptmp 1+ inc,
0 cmp,#
foundeol -branch beq,
d cmp,#
foundeol -branch beq,
jmp,

: linelen
curlinestart @ dup ( addr )
next-line 1- swap - ;

: cursor-scr-pos
cury @ 28 *
curx @ linelen min +
400 + ( addr ) ;

: hide-cursor
cursor-scr-pos
dup @ 7f and
swap c! ;

: show-cursor
insert-active 0= if
curx @ linelen 1- min curx c!
then
cursor-scr-pos
dup @ 80 or
swap c! ;

: do-load
0 bufstart 400 fill
bufstart loadb

if # file error?
bufstart 1+ eof !
0 dup dup eof @ c! curx ! cury !
CR bufstart c!
exit then

ae @ eof !
0 eof @ c! ;

: go-to-file-start
0 dup curx ! cury !
bufstart homepos !
bufstart curlinestart ! ;

: status-pos 7c0 ;

: show-page
status-pos c@
clrscr
status-pos c!
homepos @ 18 begin
swap print-line swap
1- ?dup 0= until drop ;

: clear-status ( -- )
bl status-pos 18 fill ;

: set-status ( c -- )
clear-status status-pos c! ;

: init
	0 compile-ram ! # to enable editor start from base.src
	0 blink
	80 28a c! # key repeat on

	( disable input buffering )
	0 linebuf c! 

	clear-status
;

: push-colors
d020 c@
d021 c@
286 c@

2 d021 c!
a d020 c!
1 286 c!
1 d800 400 fill ;

: cleanup ( bordercolor bgcolor cursorcolor -- )
1 linebuf c! # enable buffering
40 28a c! # key repeat off
286 c! # cursor col
d021 c! d020 c!
clrscr ;

: fit-curx-in-linelen
linelen curx @ min curx ! ;

: cur-down
curlinestart @ ( curline )
next-line dup ( 2xnextline )
eof @ >= if drop exit then
curlinestart !
cury @ 17 < if 1 cury +! else
homepos dup @ next-line swap !
1 to need-refresh then
fit-curx-in-linelen ;

: find-start-of-line
begin
1- ( addr )
dup c@ dup ( addr char char )
d = ( addr char cr? )
swap 0= ( addr cr? sof? )
or ( addr isprevline )
until
1+ ;

: cur-up
curlinestart @
1-
dup c@ 0= if drop exit then
find-start-of-line
curlinestart !
fit-curx-in-linelen
cury @ 0= if
curlinestart @ homepos !
1 to need-refresh
else
ffff cury +!
then ;

: cur-left
curx @ 0= if exit then
ffff curx +! ;

: is-eof-or-CR dup 0= swap CR = or ;
: is-whitespace dup CR = swap bl = or ;

: cur-right
editpos c@ is-eof-or-CR
editpos 1+ c@ is-eof-or-CR
or if exit then
1 curx +! ;

: eol
linelen
dup if 1- then
curx ! ;

( left, or up + eol if we're at xpos 0 )
: rewind-cur
		curx @ 0= if
			bufstart editpos <> if
				cur-up eol
			then
		else
			cur-left
		then
;

: sol 0 curx ! ;

: word-back
	rewind-cur
	begin
		editpos bufstart =
		editpos 1- c@ is-whitespace
		editpos c@ is-whitespace not and
		or not
	while
		rewind-cur
	repeat
;

# right, or down + sol if we're at EOL. ret 1 if we cant advance
: advance-cur
	editpos
	curx @ linelen 1- = linelen 0= or if
		0 curx ! cur-down
	else
		cur-right
	then
	editpos =
;

: word-fwd
	advance-cur if exit then
	begin
		editpos 1- c@ is-whitespace
		editpos c@ is-whitespace not and
		not
	while
		advance-cur if exit then
	repeat
;

: half-page-back
c begin cur-up 1- ?dup 0= until ;

: half-page-fwd
c begin cur-down 1- ?dup 0= until ;

: setcur ( x y -- )
xr ! yr ! e50c jsr ;

: goto-eof ( can be much optimized... )
bufstart eof @ = if exit then
eof @ 1- find-start-of-line
dup curlinestart ! homepos !
0 curx !
17 begin
homepos @ 1- find-start-of-line homepos !
1- dup 0=
homepos @ bufstart = or
until
17 swap - dup cury ! 0 swap setcur
1 to need-refresh ;

: goto-start
0 dup curx ! cury !
bufstart dup homepos ! curlinestart !
1 to need-refresh ;

: insert-start
1 to insert-active
[ key i ] literal set-status ;

: force-cur-right
linelen if 1 curx +! then ;

: append-start
force-cur-right
insert-start ;

: insert-stop
	curx @ if
		ffff curx +!
	then
	0 to insert-active
	clear-status
;

: show-location
	dup ( loc sol )
	begin
		dup c@ CR = if
			1+ ( loc sol )
			tuck ( sol loc sol )
			- curx !
			0 cury !
			dup homepos !
			curlinestart !
			1 to need-refresh
			clear-status
			exit
		then
		1-
	again
;

: refresh-line
	curx @
	sol
	0 cury @ setcur
	27 linelen - ( spaces )
	linelen ( spaces chars )
	begin
		?dup
	while
		editpos @ emit
		1 curx +!
		1-
	repeat
	begin
		?dup
	while
		bl emit
		1-
	repeat
	curx !
	curx @ cury @ setcur
;

: replace-char
key editpos c!
1 to need-refresh-line ;

: backspace
	curx @ 0= if exit then
	ffff curx +!
	editpos 1+
	editpos
	eof @ editpos - 1+
	ffff eof +!
	cmove
	1 to need-refresh-line
;

: del-char force-cur-right backspace ;

: join-lines
cury @ curx @ curlinestart @

editpos
cur-down
editpos = if 2drop drop exit then
sol
20 editpos 1- c! # cr => space

curlinestart ! curx ! cury !
1 to need-refresh ;

: insert-char
	dup CR <> linelen 26 > and if drop exit then

	eof @ editpos - ( u )
	dup 1- editpos + ( u src )
	over 1- editpos + 1+ ( u src dst )
	rot ( src dst u )
	cmove>
	editpos c!
	1 curx +!
	1 eof +!
	0 eof @ c!
	1 to need-refresh-line
;

: LEFT 9d ;
: DOWN 11 ;
: UP 91 ;
: RIGHT 1d ;

: insert-right
curx @ linelen 1- = if
force-cur-right else cur-right then ;

: insert-handler
	dup a0 = if drop 20 then # shift space => space

	dup
	case
    3 of drop endof # run/stop
	5f of insert-stop drop endof # leftarrow
	LEFT of cur-left drop endof
	DOWN of cur-down drop endof
	UP of cur-up drop endof
	RIGHT of insert-right drop endof
	14 of backspace drop endof # inst
	94 of del-char drop endof # del
	CR of insert-char cur-down sol show-page endof
	insert-char
	endcase
;

: push-cursor
curx @ cury @ curlinestart @ ;

: pop-cursor
curlinestart ! cury ! curx ! ;

: del-word
	1 to need-refresh-line
	begin
		editpos c@ 20 = if
			del-char exit
		then
		editpos c@ d = if exit then
		editpos c@ 0 = if exit then
		del-char
	again
;

28 allot value clipboard
var clipboard-count
0 clipboard-count !

# this can be much optimized by using cmove
: del-line
0 clipboard-count !
sol begin linelen while
# copy to clipboard
editpos c@
clipboard clipboard-count @ + c!
1 clipboard-count +!
del-char repeat
join-lines del-char
1 to need-refresh ;

: delete-handler
	[ key d ] literal set-status

	key

	case
	[ key w ] literal of del-word endof
	[ key d ] literal of del-line endof
	endcase

	clear-status
;

10 allot value search-buf

: are-equal ( len a1 a2 -- equal? )
	rot ( a1 a2 len )
	push ( a1 a2 )
	begin
		pop dup push ( a1 a2 len )
		0= if ( is len 0? )
			( matches! )
			2drop
            pop drop
			1
			exit
		then
		( a1 a2 )
		dup c@ ( a1 a2 c2 )
		rot ( a2 c2 a1 )
		dup c@ ( a2 c2 a1 c1 )
		rot ( a2 a1 c1 c2 )
		<> ( a2 a1 diff? )
		if
			( not equal!! )
			2drop ( )
            pop drop
			0
			exit
		then
		1+ ( a2 a1 )
		swap 1+ ( a1 a2 )
		pop 1- push
	again
;

: do-find ( count -- addr )
	editpos ( count a1 )
	1+ ( count a1 )
	begin
		dup eof @ = if
			drop bufstart ( count a1 )
		then
		dup editpos = if
			( not found )
			2drop
			0
			exit
		then

		dup @ search-buf @ = if
			( first char matches... examine )
			2dup ( count a1 count a1 )
			search-buf ( count a1 count a1 search-buf )
			are-equal ( count a1 equal? )
			if
				( count a1 )
				swap drop ( a1 )
				exit
			then
		then
		1+
	again
;

18 allot value drivebuf

: do-backup
	# scratch old backup
	drivebuf
	dup [ key s ] literal swap c! 1+
	dup [ key : ] literal swap c! 1+
	dup [ key . ] literal swap c! 1+
	dup
	filename swap filename-len c@ cmove
	filename-len c@ +
	CR swap c!

	drivebuf filename-len c@ 4 + openw
	closew

	# rename to new backup
	drivebuf
	dup [ key r ] literal swap c! 1+
	1+ # colon already in place...
	dup [ key . ] literal swap c! 1+
	filename-len c@ + # filename ok
	dup [ key = ] literal swap c! 1+
	dup
	filename swap filename-len c@ cmove
	filename-len c@ + # filename ok
	CR swap c!

	drivebuf filename-len c@ 2 * 5 + openw
	closew
;

: write-file
do-backup

bufstart
eof @
filename filename-len c@
saveb
1 to need-refresh ;

: save-as
	[ key ! ] literal emit
	0 ( len )
	filename ( len filename )
	begin
		key

		dup 5f = if # leftarrow
			2drop
			drop
			exit
		then
		dup CR = if
			2drop ( len )
			filename-len c!
			write-file
			exit
		then

		dup emit

		( len filename key )
		over c!

		( len filename )
		1+
		swap 1+
		swap
	again
;

: colon-w
	1 18 setcur
	[ key w ] literal emit
	key
	case
	CR of write-file endof
	[ key ! ] literal of save-as endof
	endcase
;

: find-handler
	0 18 setcur
	clear-status
	[ key / ] literal emit
	0 ( count )
	begin
		key dup
		CR <> if
			( count key )
			dup emit
			over search-buf + ( count key dst )
			c! ( count )
			1+
			0
		else
			drop
			1
		then
	until
	do-find ( count )
	?dup if
		( found! )
		show-location
	then
;

: open-line
sol CR insert-char sol
insert-start
1 to need-refresh ;

: paste-line
	open-line
	clipboard-count @
	0
	begin
		2dup <> 
	while
		clipboard over + @
		insert-char
		1+
	repeat
	2drop
	insert-stop
	sol
;

: change-word
del-word
bl insert-char
cur-left
insert-start ;

: force-cur-down
editpos
cur-down
editpos = if
eol
force-cur-right
CR insert-char
cur-down
then ;

create maintable
key i c, loc insert-start >cfa ,
key a c, loc append-start >cfa ,
key / c, loc find-handler >cfa ,
key U c, loc half-page-back >cfa ,
key D c, loc half-page-fwd >cfa ,
key J c, loc join-lines >cfa ,
key g c, loc goto-start >cfa ,
key G c, loc goto-eof >cfa ,
key $ c, loc eol >cfa ,
key 0 c, loc sol >cfa ,
key r c, loc replace-char >cfa ,
key O c, loc open-line >cfa ,
key P c, loc paste-line >cfa ,
key x c, loc del-char >cfa ,
key X c, loc backspace >cfa ,
key b c, loc word-back >cfa ,
key w c, loc word-fwd >cfa ,
key d c, loc delete-handler >cfa ,
LEFT c, loc cur-left >cfa ,
RIGHT c, loc cur-right >cfa ,
UP c, loc cur-up >cfa ,
DOWN c, loc cur-down >cfa ,
key h c, loc cur-left >cfa ,
key l c, loc cur-right >cfa ,
key k c, loc cur-up >cfa ,
key j c, loc cur-down >cfa ,
0 c,

# custom restore handler
# "fg"
here @ key f c, key g c, d c, 0 c,
here @ cli, # entry
swap dup # asm fg fg 
# compile-ram="fg"
lda,# compile-ram sta,
100/ lda,# compile-ram 1+ sta,
# lores
9b lda,# d011 sta, 17 lda,# dd00 sta,
17 lda,# d018 sta,
318 @ jmp, # jump to normal restore
: compile-run sei literal 318 ! cli
bufstart compile-ram ! ;

: main-handler ( key -- quit? )
	['] maintable ( key tableptr )

	begin
		( key tableptr )
		2dup ( key tableptr key tableptr )
		c@ = if
			( key tableptr )
			1+ @ 
			exec
			drop 0 exit
		then
		3 +

		dup c@ 0=
	until
	
	drop

	case ( key )

    [ key y ] literal of # yy
     key [ key y ] literal = if
     del-line paste-line
    then endof
	[ key o ] literal of force-cur-down open-line endof
	[ key p ] literal of force-cur-down paste-line endof
	[ key Z ] literal of
		key
		case
		[ key Z ] literal of write-file 1 blink ffff exit endof
		endcase
	endof
	[ key : ] literal of 
		[ key : ] literal set-status
		key 
		case
		[ key w ] literal of colon-w endof
		[ key q ] literal of 1 blink ffff exit endof
		endcase
		clear-status
	endof

	[ key c ] literal of
		key
		[ key w ] literal = if change-word then
	endof

	( cursor )
    # eof should be 0 terminated!
    eof @ c@ 0= assert
    # eof @ ae ! 
	88 of compile-run ffff exit endof # f7

	endcase
	0
;

: main-loop
	begin
		0 to need-refresh
		0 to need-refresh-line

		sp@ # stack check...

		show-cursor
		key
		hide-cursor

		insert-active if
			insert-handler
		else
			main-handler if 
				drop
				exit
			then
		then

		need-refresh if
			show-page
		else
			need-refresh-line if
				refresh-line
			then
		then

		sp@ 2+ = assert # warn if stack changed
	again
;

: vi
    # in case no param
    sp@ sp0 = if s" untitled" then

	init
	go-to-file-start

	# store away filename
	2dup ( str len str len )	
	filename-len c!
	filename f cmove

	do-load

	push-colors

	show-page

	main-loop

	cleanup
;

: fg # bring back editor
eof @ 0= if ." no buffer" cr exit then
# check sentinel
bufstart 1- c@ if ." err" exit then
init
push-colors
show-page
main-loop
cleanup ;

loc fg loc vi
hide-to CR
hidden hidden

