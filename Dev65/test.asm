	.EXTERN	ExtLab
	.GLOBAL GblLab
	.GLOBAL Somewhere

	.6502
	
	.CODE
	
	.EXTERN LABA
	.EXTERN LABB
	.EXTERN LABC
	
LABD .EQU (LABA-LABB)*2+LABC/3


	LDY #$23
	JMP $100

;==============================================================================
; Test size of address generated for global/external references
;------------------------------------------------------------------------------

GblLab:

	.6502
	
	JMP GblLab

;==============================================================================
; Data
;------------------------------------------------------------------------------
	.ORG $50
	.DBYTE $1,$2,$0


;==============================================================================
; Foo
;------------------------------------------------------------------------------
YoYo:
	LDY <$50, X
	INX
	JMP Somewhere

	.ORG $100

Somewhere:
	TAY
	BNE Foo
	JMP YoYo

Foo:
	LDA LABD
