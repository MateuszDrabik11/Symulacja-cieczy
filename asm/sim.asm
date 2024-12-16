segment .data	;constants
alpha dq 0.31985	;1/pi
h dq 2.0
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
treefifteen dq 315.0
sixtyfour dq 64.0
mfourfive dq -45.0
k dq 2000.0
eps dq 0.001
minus_one dq -1.0
segment .text
	global calc_density_and_pressure
	global kernel_function
	global kernel_function_derivative
	global lenght
	global kernel
	global kernel_derivative
	global calc_forces
	global gravity
	global time_integration
	global calc_pressure
	global boundries

;rdi double* chunk_start
;rsi long chunk_size
;rdx long size
;rcx double* all_positions
;r8 double* output
lenght:
		xor r9,r9
		xor r10,r10
		jmp ls
loop1:	inc r9
ls		cmp r9, rdx			;r9 - loop1 counter
		je endl
		mov rax, r9
		shl rax, 5
		add rax, rcx 
		vmovupd ymm1,[rax]
		xor r10,r10			;r10 - loop2 counter
loop2:	cmp r10, rsi
		je loop1
		mov rax ,r10
		shl rax, 5
		add rax, rdi
		vmovupd ymm0,[rax]	;a
		vsubpd ymm0,ymm1	;a-b = c
		vmulpd ymm0,ymm0	;c^2
		vhaddpd ymm0, ymm0, ymm0       	;Horizontal add in each 128-bit half
        vextractf128 xmm2, ymm0, 1     	;Extract upper half of ymm0 to xmm2
        vaddpd xmm0, xmm0, xmm2			;horizontal add of xmm0
		vsqrtpd xmm0,xmm0				;sqrt(xmm0) = xmm0
		mov rax, rdx
		imul rax,r10
		add rax, r9
		shl rax, 3
		add rax, r8
		movsd [rax], xmm0
		inc r10
		jmp loop2
endl:	ret




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


;rdi - double* lenghts
;rsi - long chunk
;rdx - long size
;rcx - double* output
kernel:
		xor r8,r8	;loop1 counter
		xor r9,r9	;loop2 counter
		movsd xmm1, [rel h]
		movsd xmm2, [rel treefifteen]
		movsd xmm3, [rel sixtyfour]
		mulsd xmm3, [rel pi]
		mulsd xmm3, xmm1
		mulsd xmm3, xmm1
		mulsd xmm3, xmm1
		mulsd xmm3, xmm1
		mulsd xmm3, xmm1
		mulsd xmm3, xmm1
		mulsd xmm3, xmm1
		mulsd xmm3, xmm1
		mulsd xmm3, xmm1
		divsd xmm2, xmm3	;xmm1 - h, xmm2 - const 315.0f / (64.0f * M_PI * pow(h, 9))
		jmp ks
loopk1:	inc r8
ks:		cmp r8, rdx
		je endk
		xor r9,r9
loopk2:	cmp r9, rsi
		je loopk1
		mov rax, rdx
		imul rax, r9
		add rax, r8
		shl rax,3
		inc r9
		movsd xmm0, [rdi + rax]
		mulsd xmm0,xmm0
		ucomisd xmm0, xmm1
		ja set0
		movsd xmm3, xmm1 
		mulsd xmm3,xmm3	;h*h
		mulsd xmm0,xmm0	;r*r
		subsd xmm3,xmm0
		movsd xmm4, xmm3
		mulsd xmm3, xmm3
		mulsd xmm3, xmm4
		movsd xmm4, xmm2
		mulsd xmm4, xmm3
		movsd [rcx + rax], xmm4
		jmp loopk2
endk:	ret
set0:	mov r10, 0
		mov [rcx + rax], r10
		jmp loopk2



;rdi - double* lenghts
;rsi - double* vector_chunk
;rdx - double* vectors
;rcx - long chunk_size
;r8  - long size
;r9  - double* output
kernel_derivative:
	xor r10,r10	;loop1 counter
	xor r11,r11	;loop2 counter
	;calculate const
	movsd xmm0, [rel mfourfive]
	movsd xmm1, [rel h]
	movsd xmm2, xmm1
	mulsd xmm2, xmm1
	mulsd xmm2, xmm1,
	mulsd xmm2, xmm2
	;h^6
	mulsd xmm2, [rel pi]
	divsd xmm0,xmm2
	;xmm0 - t1
	jmp kds
loopkd1:	inc r10
kds:		cmp r10, r8
			je endk
			xor r11,r11
			mov rax, r10
			shl rax,5
			vmovupd ymm2, [rsi + rax]	;v1
loopkd2:	cmp r11, rcx
			je loopkd1
			mov rax, r8
			imul rax, r11
			add rax, r10
			shl rax,3
			movupd xmm1, [rdi + rax]	;lenght
			movsd xmm5, xmm1
			cmpsd xmm1, [rel zero_double], 0	;1 if lenght == 0
			mov rax, r11
			shl rax,5
			vmovupd ymm3, [rsi + rax]	;v2
			vsubpd ymm3, ymm2, ymm3		;v1-v2
			movsd xmm4, [rel h]
			subsd xmm4, xmm5
			mulsd xmm4,xmm4
			mulsd xmm4,xmm0
			divsd xmm4, xmm5 ;t1*t2/lenght
			vbroadcastsd ymm4, xmm4
			vmulpd ymm3, ymm4
			mov rax, r8
			imul rax, r11
			add rax, r10
			shl rax,5
			vbroadcastsd ymm4, [rel zero_double]
			vbroadcastsd ymm5, xmm1
			vblendvpd ymm3, ymm3, ymm4, ymm5
			vmovupd [r9 + rax], ymm3
			inc r11
			jmp loopkd2


;rdi - double* masses
;rsi - double* kernels
;rdx - long particle_index
;rcx - long number_of_particles
;r8  - long chunk
;r9  - double* density
;stack - double* pressure
;xmm0 - double fluid_density
calc_density_and_pressure:	
		xor r11,r11	;loop2
		xor r10,r10	;loop1
		jmp cas
loopc1:	
		;store density
		movsd xmm6,xmm7
		cmpsd xmm7, [rel one_double], 1 ; xmm7 < 1
		vblendvpd xmm7, xmm6, [rel one_double], xmm7 
		mov rax, rdx
		add rax, r10
		shl rax, 3
		movsd [r9+rax],xmm7
		;subsd xmm7,xmm0
		;mulsd xmm7, [rel k]
		;add rax, [rbp + 16]
		;movsd [rax],xmm7
		inc r10
cas:		
		cmp r10, r8
		je endc
		xor r11,r11
		xorps xmm7, xmm7	;temp density
loopc2:
		cmp r11, rcx
		je loopc1
		mov rax, rcx
		sub rax, r11
		cmp rax, 4
		jge sum4
		;get one
		movsd xmm1, [rdi + r11 * 8]
		mov rax, r10
		add rax, rdx
		imul rax, rcx
		add rax, r11
		shl rax, 3
		mulsd xmm1, [rsi + rax]
		addsd xmm7, xmm1
		inc r11
		jmp loopc2
sum4:
		vmovupd ymm1, [rdi + r11 * 8]	;masses
		mov rax, r10
		add rax, rdx
		imul rax, rcx
		add rax, r11
		shl rax, 3
		vmovupd ymm2, [rsi + rax]		;kernels
		vmulpd ymm1,ymm2
		vhaddpd ymm1,ymm1,ymm1
		vhaddpd ymm1,ymm1,ymm1
		addsd xmm7,xmm1
		add r11, 4
		jmp loopc2
endc:
		ret
;rdi - double* masses
;rsi - double* densities
;rdx - double* kernel_derivatives
;rcx - double* kernel
;r8	 - double* velocities
;r9  - double* positions
;rbp+16 - long number_of_particles
;rbp+24 - long index
;rbp+32 - long chunk
;rbp+40 - double* accelerations
calc_forces:
		xor r11,r11	;loop2
		xor r10,r10	;loop1
		jmp fos
loopf1:	
		;calc acceleration and store
		mov rax, [rbp+24]
		add rax, r10
		shl rax,3
		movsd xmm0, [rsi + rax] 
		vbroadcastsd ymm0,xmm0
		vdivpd ymm6,ymm0
		vaddpd ymm6,ymm7
		vpxor ymm7,ymm7
		vsubpd ymm6,ymm7,ymm6
		shl rax, 2
		add rax, [rbp+40]
		vmovupd [rax], ymm6
		inc r10
fos:		
		cmp r10, [rbp + 32]
		je endf
		mov rax, [rbp+24]
		add rax, r10
		shl rax, 3
		movsd xmm0, [rdi + rax]
		movsd xmm1, [rsi+ rax]
		divsd xmm0, xmm1
		divsd xmm0, xmm1				;xmm0 - masses[start + i]/(densities[start + i]^2)
		vxorpd ymm7, ymm7,ymm7			;pressure
		vxorpd ymm6,ymm6,ymm6			;viscosity
		xor r11,r11
loopf2:
		cmp r11, [rbp + 16]
		je loopf3s	;viscosity loop

		movsd xmm1,[rdi + r11*8]	;mass
		movsd xmm2,[rsi + r11*8]	;density
		movsd xmm3,xmm1
		divsd xmm1,xmm2
		divsd xmm1,xmm2
		addsd xmm1,xmm0
		mulsd xmm1,xmm3
		vbroadcastsd ymm1,xmm1
		mov rax, [rbp+24]
		add rax, r10
		imul rax, [rbp+16]
		add rax, r11
		shl rax, 3
		vmovupd ymm2, [rdx + rax]
		vmulpd ymm2,ymm1
		vaddpd ymm7, ymm2
		inc r11
		jmp loopf2

loopf3s:
		;ymm0 - velocities[4 * (start_index + i)]
		mov rax, [rbp+24]
		add rax, r10
		shl rax, 5
		vmovupd ymm0, [r8 + rax] 
		xor r11,r11	;loop3
loopf3:		
		cmp r11, [rbp + 16]
		je loopf1
		mov rax, r11
		shl rax, 5
		vmovupd ymm1,[r8 + rax]
		vsubpd ymm1, ymm0
		mov rax, [rbp+24]
		add rax, r10
		imul rax, [rbp+16]
		add rax, r11
		shl rax, 3
		movsd xmm2, [rcx + rax]
		vbroadcastsd ymm2, xmm2
		vmulpd ymm1,ymm2
		vaddpd ymm6,ymm1 
		inc r11
		jmp loopf3
endf:
		ret


;rdi - double* positions
;rsi - double* velocities
;rdx - double* acceleration
;rcx - long start
;r8  - long chunk
;xmm0 - double dt
time_integration:
		xor r9,r9
		vbroadcastsd ymm0, xmm0
loopti:	
		cmp r9, r8
		je endti
		mov rax, rcx
		add rax, r9
		shl rax, 5
		vmovupd ymm1, [rdx + rax]
		vmulpd ymm1, ymm0	;ymm1 = dt * a
		vmovupd ymm2, [rsi + rax]
		vaddpd ymm2, ymm1
		vmulpd ymm3, ymm0, ymm2	;ymm3 = dt * v
		vmovupd [rsi + rax], ymm1
		vmovupd ymm4, [rdi + rax]
		vaddpd ymm3,ymm4
		vmovupd [rdi + rax], ymm3
		inc r9
		jmp loopti
endti:
		ret
;rdi - double* acceleration
;rsi - long start
;rdx - long chunk
;xmm0 - double g
gravity:
		xor r8,r8	;loop
loopg:	
		cmp r8, rdx
		je endg
		mov rax, rsi
		add rax, r8
		shl rax, 2
		add rax, 2
		shl rax, 3
		movsd xmm1, [rdi + rax]
		subsd xmm1, xmm0
		movsd [rdi + rax], xmm1
		inc r8
		jmp loopg
endg:	ret


;rdi - double* density
;rsi - long index
;rdx - double* pressure
;rcx - long chunk
;xmm0 - double fluid_density
calc_pressure:
		xor r8, r8
loopp:	cmp r8, rcx
		je endp
		mov rax,rsi
		add rax,r8
		shl rax, 3
		movsd xmm1, [rdx + rax]
		subsd xmm1,xmm0
		mulsd xmm1,[rel k]
		movsd [rdx + rax], xmm1
		inc r8
		jmp loopp
endp:	ret 

;rdi - double* positions
;rsi - double* velocities
;rdx - long index
;rcx - long chunk
;xmm0 - x_max
;xmm1 - y_max
;xmm2 - z_max
;xmm3 - bouncines
boundries:
		xor r8, r8 ;loop
loopb:	cmp r8, rcx
		je endb
		mov rax, rdx
		add rax, r8
		shl rax, 5
		movsd xmm5, [rdi + rax]
		movsd xmm6, xmm5
		cmpsd xmm5, [rel zero_double], 1 ;less than zero
		movsd xmm7, xmm5
		cmpsd xmm6, xmm0, 6 ;more than x_max
		por xmm5, xmm6	;xmm5 = xmm5 or xmm6
		movq r9, xmm5
		test r9, 1
		jz dont_reverse_x
		movsd xmm5 ,[rsi + rax]
		mulsd xmm5, [rel minus_one]
		mulsd xmm5, xmm3
		movsd [rsi + rax], xmm5

dont_reverse_x:		movsd xmm5,xmm7
		movsd xmm6, xmm0
		subsd xmm6, [rel eps]
		minsd xmm7, xmm6	;xmm7 = min(xmm7,xmm6)
		xorps xmm6,xmm6
		addpd xmm6, [rel eps]
		maxsd xmm7, xmm6 	;xmm7 = max(xmm7,xmm6)
		movsd [rdi + rax], xmm7

		movsd xmm5, [rdi + rax+8]
		movsd xmm6, xmm5
		cmpsd xmm5, [rel zero_double], 1 ;less than zero
		movsd xmm7, xmm5
		cmpsd xmm6, xmm1, 6 ;more than y_max
		por xmm5, xmm6	;xmm5 = xmm5 or xmm6
		movq r9, xmm5
		test r9, 1
		jz dont_reverse_y
		movsd xmm5 ,[rsi + rax+8]
		mulsd xmm5, [rel minus_one]
		mulsd xmm5, xmm3
		movsd [rsi + rax+8], xmm5

dont_reverse_y:		movsd xmm5,xmm7
		movsd xmm6, xmm0
		subsd xmm6, [rel eps]
		minsd xmm7, xmm6	;xmm7 = min(xmm7,xmm6)
		xorps xmm6,xmm6
		addpd xmm6, [rel eps]
		maxsd xmm7, xmm6 	;xmm7 = max(xmm7,xmm6)
		movsd [rdi + rax+8], xmm7

		movsd xmm5, [rdi + rax+16]
		movsd xmm6, xmm5
		cmpsd xmm5, [rel zero_double], 1 ;less than zero
		movsd xmm7, xmm5
		cmpsd xmm6, xmm2, 6 ;more than z_max
		por xmm5, xmm6	;xmm5 = xmm5 or xmm6
		movq r9, xmm5
		test r9, 1
		jz dont_reverse_z
		movsd xmm5 ,[rsi + rax+16]
		mulsd xmm5, [rel minus_one]
		mulsd xmm5, xmm3
		movsd [rsi + rax+16], xmm5

dont_reverse_z:		movsd xmm5,xmm7
		movsd xmm6, xmm0
		subsd xmm6, [rel eps]
		minsd xmm7, xmm6	;xmm7 = min(xmm7,xmm6)
		xorps xmm6,xmm6
		addpd xmm6, [rel eps]
		maxsd xmm7, xmm6 	;xmm7 = max(xmm7,xmm6)
		movsd [rdi + rax+16], xmm7

		inc r8
		jmp loopb
endb: 	ret		
