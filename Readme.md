# A Yaml 1.2 library for .Net

This is a fork of [YamlSerializer](https://yamlserializer.codeplex.com/).

Original code by Osamu TAKEUCHI <osamu@big.jp>

Forked by Melvyn Laïly <melvyn.laily@gmail.com>

## Main changes from the original implementation

* Implemented full Unicode support (code points values > 16 bits)
* Updated to the latest spec version (Yaml 1.2 3rd Edition, Patched at 2009-10-01) (Most of the work was already done)

## Notes

##### Straight from the original doc

Currently, this parser violates the YAML 1.2 specification in the following points.
- line breaks are not normalized.
- omission of the final line break is allowed in plain / literal / folded text.
- ':' followed by ns-indicator is excluded from ns-plain-char.

## License

This project is distributed under the MIT license as follows:

The MIT License (MIT)
Original code (before 2016) Copyright (c) 2009 Osamu TAKEUCHI <osamu@big.jp>
Modifications are Copyright (c) Melvyn Laïly 2016 <melvyn.laily@gmail.com>


Permission is hereby granted, free of charge, to any person obtaining a copy of 
this software and associated documentation files (the "Software"), to deal in the 
Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
and to permit persons to whom the Software is furnished to do so, subject to the 
following conditions:

The above copyright notice and this permission notice shall be included in all 
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A 
PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
