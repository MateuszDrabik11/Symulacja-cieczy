asm_files = $(wildcard ./asm/*.asm)
obj_files = $(asm_files:.asm=.o)
so_file = libasm.so

cpp_files = $(wildcard ./cpp/*.cpp)
cpp_obj = $(cpp_files:.cpp=.o)
cpp_file = libc.so

build: $(so_file) $(cpp_file)

$(so_file): $(obj_files)
	@gcc -O3 -g -fPIC -shared -o $(so_file) $(obj_files)

%.o: %.asm
	@nasm -g -f elf64 -o $@ $<

$(cpp_file): $(cpp_obj)
	@g++ -O3 -g -shared -o $(cpp_file) $(cpp_obj)

%.o: %.cpp
	@g++ -O3 -g -fPIC -c -o $@ $<

clean:
	@rm -f ./asm/*.o ./cpp/*.o *.so

.PHONY: clean
.PHONY: build
