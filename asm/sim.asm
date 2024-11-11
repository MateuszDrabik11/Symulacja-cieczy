segment .data	;constants
alpha dq 0.31985	;1/pi
h dq 1.0
zero_double dq 0.0
one_double dq 1.0
two_double dq 2.0
three_two dq 1.5
minus_one_two dq -0.5
quarter dq 0.25
three_four dq 0.75
pi dq 3.1416
quarter_pi dq 0.7854
minus_three dq -3.0
nine_for dq 2.25
minus_three_for dq -0.75
segment .text
	global calc_density_and_pressure
	global kernel_function
	global kernel_function_derivative
	global distance_between_two_points
	global lenght_no_avx

calc_density_and_pressure:	
	mov rax, [rdi]
	ret


;rdi	double[][]	in chunk, [i][xi,yi,zi,0]
;rsi	long		in chunk_size
;rdx	double[][]	in vectors
;rcx 	long 		in count
;r8		double[][]	out lenghts
distance_between_two_points:
		;lenght of a - b vector
		;sqrt((a.x-b.x)^2+(a.y-b.y)^2)
		vmovupd ymm1,[rdx]	;b
loop:	vmovupd ymm0,[rdi]	;a
		vsubpd ymm0,ymm1	;a-b = c
		vmulpd ymm0,ymm0	;c^2
		vhaddpd ymm0, ymm0, ymm0       	;Horizontal add in each 128-bit half
        vextractf128 xmm2, ymm0, 1     	;Extract upper half of ymm0 to xmm1
        vaddpd xmm0, xmm0, xmm2			;horizontal add of xmm0
		vsqrtpd xmm0,xmm0				;sqrt(xmm0) = xmm0
		movsd [rcx], xmm0
		add rdi, 32
		add rcx, 8
		dec rsi
		jnz loop
		ret

lenght_no_avx:
			;rdi	double[][]	in start, [i][xi,yi,zi,0]
			;rsi	long		in end
			;rdx	double[]	in base_position
			;rcx	double[]	out lenghts
loop1:		movsd xmm0,qword [rdi]	;x
			subsd xmm0,qword [rdx]	
			mulsd xmm0,xmm0		;(x0-x1)^2 = xmm0
			movsd xmm1,qword [rdi+8]	;y
			subsd xmm1,qword [rdx+8]	
			mulsd xmm1,xmm1		;(y0-y1)^2 = xmm1
			movsd xmm2,qword [rdi+16]	;<second>
			subsd xmm2,qword [rdx+16]	
			mulsd xmm2,xmm2		;(z0-z1)^2 = xmm2
			addsd xmm0,xmm1
			addsd xmm0,xmm2		;xmm0 = (x0-x1)^2 + (y0-y1)^2 + (z0-z1)^2
			sqrtsd xmm0,xmm0
			movsd [rcx],xmm0
			add rdi,32
			add rcx,8
			dec rsi
			jnz loop1
			ret


;assumption r > 0
;rdi - double* start
;rsi - int count
;rdx - double* output
kernel_function:
loopk:
	vmovupd ymm0, [rdi]             			; ymm0 = r
	vbroadcastsd ymm1, [rel zero_double]		; ymm1 = 0

	vbroadcastsd ymm2, [rel h]      			; ymm2 = h
	vaddpd ymm2, ymm2, ymm2         			; ymm2 = 2 * h
	vcmppd ymm3, ymm0, ymm2, 1      			; ymm0 < ymm2 = ymm3, ymm3 = r < 2h

	vbroadcastsd ymm2, [rel h]
	vdivpd ymm1, ymm0, ymm2         			; ymm1 = q = r / h

	vmulpd ymm3, ymm2, ymm2         			; ymm2 = h^2
	vmulpd ymm3, ymm3, ymm2      				; ymm2 = h^3
	vbroadcastsd ymm2, [rel pi]
	vmulpd ymm3, ymm3, ymm2						; ymm3 = pi * h^3 

	; Condition 1: q <= 1 (calculate (1 - 1.5*q^2 + 0.75*q^3) * π / h^3)
	vbroadcastsd ymm9 , [rel one_double]
	vcmppd ymm6, ymm1, ymm9, 1 					; ymm6 = ymm1 < ymm9, ymm6 = q <1
	vmulpd ymm2, ymm1, ymm1         			; ymm2 = q^2
	vbroadcastsd ymm8, [rel three_two]
	vmulpd ymm2, ymm2, ymm8     				; ymm3 = 1.5 * q^2
	vbroadcastsd ymm5, [rel one_double]
	vsubpd ymm2, ymm5, ymm2         			; ymm3 = 1 - 1.5 * q^2

	vmulpd ymm4, ymm1, ymm1         			; ymm4 = q^2
	vmulpd ymm4, ymm4, ymm1 					; ymm4 = q^3
	vbroadcastsd ymm8, [rel three_four]
	vmulpd ymm4, ymm4, ymm8						; ymm4 = 0.75 * q^3
	vaddpd ymm2, ymm2, ymm4         			; ymm3 = 1 - 1.5*q^2 + 0.75*q^3

	vdivpd ymm2, ymm2, ymm3         			; Final result for q <= 1: (1 - 1.5*q^2 + 0.75*q^3) /( π * h^3)

	vblendvpd ymm6, ymm6, ymm2, ymm6 			; ymm6 = ymm2 if true else 0

	; Condition 2: 1 < q <= 2 (calculate (0.25 * π * (2 - q)^3) / h^3)
	vbroadcastsd ymm9, [rel two_double]
	vcmppd ymm7, ymm1, ymm9, 2      			; ymm7 = q <= 2
	vbroadcastsd ymm9, [rel one_double]
	vcmppd ymm8, ymm9, ymm1, 1 				 	; ymm8 = q > 1
	vandpd ymm7, ymm7, ymm8         			; ymm7 = 1 < q <= 2

	vbroadcastsd ymm5, [rel two_double]
	vsubpd ymm1, ymm5, ymm1         			; ymm1 = (2 - q)
	vmulpd ymm5, ymm1, ymm1         			; ymm5 = (2 - q)^2
	vmulpd ymm5, ymm5, ymm1         			; ymm5 = (2 - q)^3
	vbroadcastsd ymm8, [rel quarter]
	vmulpd ymm5, ymm5, ymm8						; ymm5 = 0.25 * (2 - q)^3
	vdivpd ymm5, ymm5, ymm3         			; ymm5 = 0.25 * (2 - q)^3/(pi * h^3)

	vblendvpd ymm7, ymm7, ymm5, ymm7 			; ymm7 = ymm5 if true else 0
	vaddpd ymm0, ymm6, ymm7         			; Final result in ymm0
	vmovupd [rdx], ymm0             			; Store result
	add rdi,32
	add rdx,8
	dec rsi
	jnz loopk
	ret

;rdi - double* vector
;rsi - int count
;rdx - double* derivative
;xmm0 - double lenght
kernel_function_derivative:
loopkd:
	movsd xmm1, xmm0						;xmm1 = length
	movsd xmm7, xmm1						;xmm7 = length
	vmovupd ymm0, [rdi]						;ymm1 = vector
	movsd xmm2, [rel h]
	addsd xmm2, xmm2						;xmm2 = 2h
	cmpsd xmm1, xmm2 , 1					;xmm1 = xmm1 < xmm2, r<2h
	movsd xmm3, xmm7						;xmm3 = r
	divsd xmm3, [rel h]						;xmm3 = q = r/h
	movsd xmm4, xmm3
	cmpsd xmm4, [rel one_double], 1			;xmm4 = xmm4 < 1.0, q < 1
	movsd xmm5, xmm3
	mulsd xmm5, [rel minus_three]			;xmm5 = -3q
	movsd xmm6, xmm3
	mulsd xmm6,xmm6							
	mulsd xmm6, [rel nine_for]				;xmm6 = 2.25q^2
	addsd xmm5,xmm6							;xmm5 = -3q + 2,25q^2
	mulsd xmm7,[rel pi]
	mulsd xmm7,[rel h]
	mulsd xmm7,[rel h]
	mulsd xmm7,[rel h]
	mulsd xmm7,[rel h]						;xmm7 = r * pi * h^4
	mulsd xmm5, xmm7						;xmm5 = -3q + 2,25q^2 / r * pi * h^4
	movsd xmm8, xmm3
	cmpsd xmm8, [rel two_double], 2			;xmm8 = xmm8 <= 2.0, q <= 2.0
	movsd xmm9, xmm3
	movsd xmm10, [rel one_double]
	cmpsd xmm10, xmm9, 2					;xmm10 = 1 <= q
	andpd xmm9, xmm10						;xmm9 = 1 <= q <= 2
	movsd xmm11, [rel two_double]
	subsd xmm11, xmm3
	mulsd xmm11,xmm11						;xmm11 = (2-q)^2
	mulsd xmm11, [rel minus_three_for]		;xmm11 = -0,75*(2-q)^2
	divsd xmm11, xmm7						;xmm11 = -0,75*(2-q)^2 / r * pi * h^4
	;xmm9 = 1 <= q <= 2
	;xmm4 = q < 1
	;xmm1 = r < 2h
	;xmm5 = -3q + 2,25q^2 / r * pi * h^4
	;xmm11 = -0,75*(2-q)^2 / r * pi * h^4
	vbroadcastsd ymm2, xmm5
	vmulpd ymm3, ymm0, ymm2					;ymm3 - result for q < 1
	vbroadcastsd ymm2, xmm11
	vmulpd ymm2, ymm0, ymm2					;ymm2 - result for 1 <= q <= 2
	vxorpd ymm0,ymm0,ymm0					;ymm0 - result for r > 2h, [0,0,0,0]

	vbroadcastsd ymm4, xmm4
	vblendvpd ymm5, ymm3, ymm2, ymm4        ;ymm5 = result for q < 1 if true, else 1 <= q <= 2 result
	vbroadcastsd ymm9, xmm9
	vblendvpd ymm6, ymm5, ymm0, ymm9		;ymm6 = ymm5 if r < 2h else 0
	vmovupd [rsi], ymm6
	add rdi,32
	add rdx,8
	dec rsi
	jnz loopkd
	ret