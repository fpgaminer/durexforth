

( Calls Basic/Kernal routines.
  Uses ar, xr, yr,sr for register I/O.
  The Forth stack is temporarily 
  stored away so that CHRGET is in 
  place for Basic calls. )
30c value ar 30d value xr
30e value yr 30f value sr
:asm jsr ( addr -- )
sp0 lda,x 14 sta, sp1 lda,x 15 sta,
txa, pha,
e130 jsr, # perform [sys]
pla, tax, inx, ;asm
