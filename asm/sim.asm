segment .data	;constants
alpha dq 0.31985	;1/pi
h dq 0.001
zero_double dq 0.0
one_double dq 1.0
two_double dq 2.0
three_two dq 1.5
minus_one_two dq -0.5
segment .text
	global increment_array
	global calc_density_and_pressure
	global kernel_function
	global distance_between_two_points

temp:
	mov rax, [rel alpha]
	ret

increment_array:
Loop:	mov rax, [rdi + 4*rsi-4]
		inc rax
		mov [rdi + 4*rsi-4], rax
		dec rsi
		test rsi,rsi
		jnz Loop
		ret

;args
;0	masses* double,
;1	positions** double,
;2 	this_particle_position* double
;4	out density double
;5	out pressure double
calc_density_and_pressure:	
	mov rax, [rdi]
	ret

;args
;0	a* double
;1	b* double
;out double
distance_between_two_points:
	;lenght of a - b vector
	;sqrt((a.x-b.x)^2+(a.y-b.y)^2)	
	vmovupd ymm0,[rdi]	;a
	vmovupd ymm1,[rsi]	;b
	vsubpd ymm0,ymm1	;a-b = c
	vmulpd ymm0,ymm0	;c^2
	vextractf128 xmm2, ymm0, 1 ;upper half of ymm0to xmm2
	vaddpd xmm0, xmm0,xmm2	;xmm0 + xmm2 = xmm0
	vhaddpd xmm0,xmm0,xmm0	;horizontal add of xmm0
	vsqrtpd xmm0,xmm0		;sqrt(xmm0) = xmm0
	ret

;in xmm0 r
;out xmm0 double
kernel_function:
	movsd xmm1,[rel h]
	addsd xmm1,xmm1
	ucomisd xmm0, xmm1	;compare xmm0 = r and xmm1 = 2*h
	ja kf_return_zero
	divsd xmm0,xmm1		;xmm0 = q = r/h
	movsd xmm1,[rel two_double]
	ucomisd xmm0,xmm1	;compare q and 2.0
	ja kf_return_zero	;greater than 2
	jb kf_compare_with_one	;less than 2
kf_compare_with_one:
	movsd xmm1,[rel one_double]
	ucomisd xmm0,xmm1
	ja kf_return_between_one_two	;greater than 1 less than 2
	jb kf_compare_with_zero			;less than 1
kf_compare_with_zero:
	movsd xmm1,[rel zero_double]
	ucomisd xmm0,xmm1
	ja kf_return_between_zero_one	;less than 1 greater than 0
	jb kf_return_zero				;less than 0

kf_return_zero:
	movsd xmm0,[rel zero_double]
	ret
kf_return_between_zero_one:
	;xmm0 = q
	movsd xmm1,[rel one_double]
	movsd xmm2,xmm0
	movsd xmm3,[rel three_two]
	mulsd xmm2,xmm3
	mulsd xmm2,xmm0
	subsd xmm1,xmm2		;xmm1 = 1 - 3/2q^2
	movsd xmm3,[rel minus_one_two]
	mulsd xmm2,xmm3
	addsd xmm1,xmm2		;xmm1 = 1 - 3/2q^2 + 3/4q^2
	mulsd xmm1,xmm0		;xmm1 = 1 - 3/2q^2 + 3/4q^3
	movsd xmm3,[rel alpha]
	movsd xmm0,xmm3
	movsd xmm3,[rel h]
	divsd xmm0,xmm3
	divsd xmm0,xmm3
	divsd xmm0,xmm3
	mulsd xmm0,xmm1
	ret


kf_return_between_one_two:
	movsd xmm1,[rel two_double]
	subsd xmm1,xmm0
	mulsd xmm1,xmm1
	mulsd xmm1,xmm1
	mulsd xmm1,xmm1
	divsd xmm1,[rel two_double]
	divsd xmm1,[rel two_double]
	movsd xmm0,xmm1
	ret