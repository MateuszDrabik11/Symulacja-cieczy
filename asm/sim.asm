segment .data
msg: db "Hello World", 0Ah
len: equ $-msg
segment .text
	global _start
	global increment_array

_start:
	mov rax, 1
	mov rdi, 1
	mov rsi, msg
	mov rdx, len
	syscall
    ret

increment_array:
Loop:	mov rax, [rdi + 4*rsi-4]
		inc rax
		mov [rdi + 4*rsi-4], rax
		dec rsi
		test rsi,rsi
		jnz Loop
		ret
