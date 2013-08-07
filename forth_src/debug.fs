

: c. ff and dup 10 < if
[ key 0 ] literal emit then . ;
: id. 2+ ( skip over link ptr )
dup c@ 3f and ( length )
swap 1+ tuck + swap do i c@ emit loop ;
: cfa> ( codepointer -- word )
latest @ begin ?dup while
2dup > if nip exit then
@ repeat drop 0 ;

: see
	word find
    dup 0= if exit then
	here
	latest @
	begin
		2 pick
		over
		<>
	while
		nip
		dup @
	repeat
	
	drop
	swap ( end-of-word start-of-word )

	[ key : ] literal emit space dup id. space
	dup 2+ c@ 80 and if ." immed " then

	>dfa ( get data addr )

	begin
		2dup >
	while
		dup @

		case
		' lit of
			2+ dup @ .
		endof
		' litc of
			2+ dup c@ . 1-
		endof
		' litstring of
			[ key s ] literal emit '"' emit space
			2+ dup 1+ over c@ tell
			'"' emit space
            dup c@ + 1-
		endof
		' ' of
			[ key ' ] literal emit space
			2+ dup @
			cfa> id. space
		endof
		' (loop) of
			." (loop) ( "
			2+ dup @ .
			." ) "
		endof
		' branch of
			." branch ( "
			2+ dup @ .
			." ) "
		endof
		' 0branch of
			." 0branch ( "
			2+ dup @ .
			." ) "
		endof
		' exit of
			2dup
			2+
			<> if
				." exit "
			then
		endof
			( default )
			dup
			cfa> id. space
		endcase
		2+
	repeat
	[ key ; ] literal emit cr
	2drop
;

( c a b within returns true if a <= c and c < b )
: within -rot over
<= if > else 2drop 0 then ;

var last-dump

: dump ( addr -- )
8 0 do dup . space
dup 8 0 do dup c@ c. 1+ loop drop
8 0 do dup c@
dup 20 5e within 0= if
drop [ key . ] literal
then emit 1+ loop cr loop
last-dump ! ;

: n last-dump @ dump ;

: words
latest @
begin ?dup while
dup ?hidden 0= if
dup id. space
then @ repeat
cr ;

# size foo prints size of foo
: size ( -- )
word find >r
here latest @ # prev curr
begin dup while
dup r@ = if
- . r> drop exit then
nip dup @ repeat
. drop r> drop ;

: assert 0= if
begin 1 d020 +! again then ;

