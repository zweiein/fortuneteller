// This is the main DLL file.

#include "stdafx.h"

#include "neural_indicator_interop.h"

double __stdcall Summ(double _1, double _2){
	return neural_indicator::Class1::Summ(_1, _2);
}