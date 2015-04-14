# Musings on JIT performance

OK, graph first (courtesy of [ChartGo](http://www.chartgo.com/)):

![ryujit-perf-chart-0.png](/images/ryujit-perf-chart-0.png?raw=true)

**Test environment:**

* Intel Core i7-2600K @ 3.4 GhZ (stock)
* Windows 8.1 Pro x64 (Build 9600)
* Old JIT was the .NET 4.5 JIT
* New JIT was [RyuJIT CTP5 version launched Oct 2014](http://blogs.msdn.com/b/clrcodegeneration/archive/2014/10/31/ryujit-ctp5-getting-closer-to-shipping-and-with-better-simd-support.aspx))
* Numbers are an average of 3 test sessions (they were quite stable)
* Each session consisted of 3000 iterations per pair of (RDP error, fit error) parameters, for a total of 45000 iterations (see the raw data below for the breadown)
* The test data used was the same 966-point data set as when you launch the sample program (kind of looks like a dude with glasses)
* [Source code for test program](/burningmime.curves.perftest/src/Program.cs)
* [Batch file I used to run it with different configs](/runtests.bat)

**Results:**

*(The raw data is included at the end of the file if you want to look)*

|JIT Version             | Average time (seconds) | Speedup                                                     |
|------------------------|-----------------------:|-------------------------------------------------------------|
| Old JIT                | 20.383                 |                                                             |
| RyuJIT CTP5 no SIMD    | 18.540                 | 1.1x faster than old JIT                                    |
| RyuJIT CTP5 with SIMD  | 5.952                  | 3.4x faster than old JIT, 3.1x faster than new JIT w/o SIMD |

It's not without its bugs (I found [one while writing this app](https://connect.microsoft.com/VisualStudio/feedback/details/1199670/ryujit-ctp5-sse-methodimpl-methodimploptions-aggressiveinlining-causes-bad-codegen-in-certain-cases)),
but the performance of the SIMD is stellar -- more than a 3x speed up over the scalar version, despite the fact that it's only using 2-component vectors max. I'd love to see some results for other processors/systems...
maybe Core i7 just has a good SSE unit. Unfortunately, setting up RyuJIT is still a pain in the area, so I'm not going to badger everyone I know to run tests on their PCs.

### Why?

The theoretical maximum advantage you could get simply through the parallelization aspect would be 2x since it only uses Vector2s and no larger vector types. It *could* be packing two Vector2s into one
XMM register, but none of the disassembly I've looked at has been doing that. Another possibility (which appears true) is that it uses (and therefore spills) fewer registers. Can this account for the 3x
speedup? Maybe.

From what I can tell, it seems the SIMD version is sometimes longer. For example,
a very simple call to `Distance` in [CurvePreprocess.RdpRecursive](https://github.com/burningmime/curves/blob/7941812bc3f5a15e3a053fa10ec7db4a2dd1318c/burningmime.curves/src/CurvePreprocess.cs#L158)
yields this with SIMD off and the RyuJIT CTP5:

```assembly
 movss       xmm0,dword ptr [rsp+48h]  
 movss       xmm2,dword ptr [rsp+4Ch]  
 movss       xmm3,dword ptr [rsp+40h]  
 movss       xmm4,dword ptr [rsp+44h]  
 xorps       xmm5,xmm5  
 movss       dword ptr [rsp+30h],xmm5  
 xorps       xmm5,xmm5  
 movss       dword ptr [rsp+34h],xmm5  
 subss       xmm0,xmm3  
 subss       xmm2,xmm4  
 mulss       xmm0,xmm0  
 mulss       xmm2,xmm2  
 addss       xmm0,xmm2  
 cvtss2sd    xmm0,xmm0  
 sqrtsd      xmm0,xmm0  
 cvtsd2ss    xmm0,xmm0  
 movss       xmm6,xmm0  
```

Yes, this is SIMD off. The new JIT uses XMM registers for floats even without FeatureSIMD. And this is the ugly mess produced with SIMD on:

```assembly
 movss       xmm0,dword ptr [rsp+68h]  
 movss       dword ptr [rsp+50h],xmm0  
 movss       xmm0,dword ptr [rsp+6Ch]  
 movss       dword ptr [rsp+54h],xmm0  
 movss       xmm0,dword ptr [rsp+60h]  
 movss       dword ptr [rsp+48h],xmm0  
 movss       xmm0,dword ptr [rsp+64h]  
 movss       dword ptr [rsp+4Ch],xmm0  
 movsd       xmm0,mmword ptr [rsp+50h]  
 movsd       mmword ptr [rsp+40h],xmm0  
 movsd       xmm0,mmword ptr [rsp+48h]  
 movsd       mmword ptr [rsp+38h],xmm0  
 xorps       xmm0,xmm0  
 movsd       mmword ptr [rsp+30h],xmm0  
 movsd       xmm0,mmword ptr [rsp+40h]  
 movsd       xmm2,mmword ptr [rsp+38h]  
 subps       xmm0,xmm2  
 movsd       mmword ptr [rsp+30h],xmm0  
 movsd       xmm0,mmword ptr [rsp+30h]  
 movsd       xmm2,mmword ptr [rsp+30h]  
 mulps       xmm0,xmm2  
 movaps      xmm3,xmm0  
 shufps      xmm3,xmm3,0B1h  
 addps       xmm0,xmm3  
 movaps      xmm3,xmm0  
 shufps      xmm3,xmm3,1Bh  
 addps       xmm0,xmm3  
 cvtss2sd    xmm0,xmm0  
 sqrtsd      xmm0,xmm0  
 cvtsd2ss    xmm0,xmm0  
 movss       xmm6,xmm0  
```

Basically it seems like it moves everything into place so that it can do some aligned reads then
use the parallel versions of the instructions. And this fervid juggling of registers isn't an isolated artifact of this function -- other functions 
that call Distance() are greeted with similar code. In each case, the SIMD one is much longer with lots of unnecessary spilling of things to the stack 
then picking them up again. I'd expect on the CPU level, all of this is in cache or hardware registers, but it's still more instructions. Maybe the microcode...
instruction-level parallelism... ah, I give up! I'll leave the figurins to people smarter than me. The SIMD version is more than 3x as fast, which is good enough for me.

**tl;dr:** [It makes no sense!](https://www.youtube.com/watch?v=xwdba9C2G14)

### Appendix: Raw test results

#### Session 1

```
typeof(VECTOR)                          System.Numerics.Vector2
RyuJIT enabled                          False
SIMD enabled                            False
Iterations Per Test                     3000
Test Data Size                          966

RDP Error  Fit Error  Points     Curves     Time (s)   Time Per Iter (ms)
---------  ---------  ------     ------     --------   ------------------
1          4          77         14         2.5167     0.8389    
1          8          77         11         2.2495     0.7498    
1          16         77         10         2.1514     0.7171    
2          4          56         16         1.9082     0.6361    
2          8          56         11         1.6909     0.5636    
2          16         56         8          1.4808     0.4936    
4          4          39         15         1.3987     0.4662    
4          8          39         13         1.3112     0.4371    
4          16         39         9          1.1886     0.3962    
8          4          28         13         0.9710     0.3237    
8          8          28         12         0.9399     0.3133    
8          16         28         7          0.8444     0.2815    
16         4          17         10         0.6005     0.2002    
16         8          17         10         0.6004     0.2001    
16         16         17         8          0.5636     0.1879    

TOTAL TIME: 20.4159957

typeof(VECTOR)                          System.Numerics.Vector2
RyuJIT enabled                          True
SIMD enabled                            False
Iterations Per Test                     3000
Test Data Size                          966

RDP Error  Fit Error  Points     Curves     Time (s)   Time Per Iter (ms)
---------  ---------  ------     ------     --------   ------------------
1          4          77         14         2.4250     0.8083    
1          8          77         11         2.1467     0.7156    
1          16         77         10         2.0465     0.6822    
2          4          56         16         1.7989     0.5996    
2          8          56         11         1.5697     0.5232    
2          16         56         8          1.3416     0.4472    
4          4          39         15         1.2529     0.4176    
4          8          39         13         1.1778     0.3926    
4          16         39         9          1.0576     0.3525    
8          4          28         13         0.8367     0.2789    
8          8          28         12         0.8026     0.2675    
8          16         28         7          0.7030     0.2343    
16         4          17         10         0.4779     0.1593    
16         8          17         10         0.4778     0.1593    
16         16         17         8          0.4384     0.1461    

TOTAL TIME: 18.552881

typeof(VECTOR)                          System.Numerics.Vector2
RyuJIT enabled                          True
SIMD enabled                            True
Iterations Per Test                     3000
Test Data Size                          966

RDP Error  Fit Error  Points     Curves     Time (s)   Time Per Iter (ms)
---------  ---------  ------     ------     --------   ------------------
1          4          77         14         0.6612     0.2204    
1          8          77         11         0.5966     0.1989    
1          16         77         10         0.5737     0.1912    
2          4          56         16         0.5306     0.1769    
2          8          56         11         0.4734     0.1578    
2          16         56         8          0.4205     0.1402    
4          4          39         15         0.4083     0.1361    
4          8          39         13         0.3881     0.1294    
4          16         39         9          0.3552     0.1184    
8          4          28         13         0.3123     0.1041    
8          8          28         12         0.3028     0.1009    
8          16         28         7          0.2732     0.0911    
16         4          17         10         0.2192     0.0731    
16         8          17         10         0.2194     0.0731    
16         16         17         8          0.2077     0.0692    

TOTAL TIME: 5.9422946
```

#### Session 2

```
typeof(VECTOR)                          System.Numerics.Vector2
RyuJIT enabled                          False
SIMD enabled                            False
Iterations Per Test                     3000
Test Data Size                          966

RDP Error  Fit Error  Points     Curves     Time (s)   Time Per Iter (ms)
---------  ---------  ------     ------     --------   ------------------
1          4          77         14         2.5447     0.8482    
1          8          77         11         2.2611     0.7537    
1          16         77         10         2.1521     0.7174    
2          4          56         16         1.9078     0.6359    
2          8          56         11         1.6920     0.5640    
2          16         56         8          1.4692     0.4897    
4          4          39         15         1.3688     0.4563    
4          8          39         13         1.3177     0.4392    
4          16         39         9          1.2059     0.4020    
8          4          28         13         0.9613     0.3204    
8          8          28         12         0.9276     0.3092    
8          16         28         7          0.8343     0.2781    
16         4          17         10         0.5934     0.1978    
16         8          17         10         0.5933     0.1978    
16         16         17         8          0.5566     0.1855    

TOTAL TIME: 20.3857511

typeof(VECTOR)                          System.Numerics.Vector2
RyuJIT enabled                          True
SIMD enabled                            False
Iterations Per Test                     3000
Test Data Size                          966

RDP Error  Fit Error  Points     Curves     Time (s)   Time Per Iter (ms)
---------  ---------  ------     ------     --------   ------------------
1          4          77         14         2.4163     0.8054    
1          8          77         11         2.1520     0.7173    
1          16         77         10         2.0343     0.6781    
2          4          56         16         1.7948     0.5983    
2          8          56         11         1.5659     0.5220    
2          16         56         8          1.3358     0.4453    
4          4          39         15         1.2518     0.4173    
4          8          39         13         1.1767     0.3922    
4          16         39         9          1.0560     0.3520    
8          4          28         13         0.8371     0.2790    
8          8          28         12         0.8032     0.2677    
8          16         28         7          0.7032     0.2344    
16         4          17         10         0.4798     0.1599    
16         8          17         10         0.4904     0.1635    
16         16         17         8          0.4501     0.1500    

TOTAL TIME: 18.5475115

typeof(VECTOR)                          System.Numerics.Vector2
RyuJIT enabled                          True
SIMD enabled                            True
Iterations Per Test                     3000
Test Data Size                          966

RDP Error  Fit Error  Points     Curves     Time (s)   Time Per Iter (ms)
---------  ---------  ------     ------     --------   ------------------
1          4          77         14         0.6635     0.2212    
1          8          77         11         0.5991     0.1997    
1          16         77         10         0.5760     0.1920    
2          4          56         16         0.5328     0.1776    
2          8          56         11         0.4747     0.1582    
2          16         56         8          0.4213     0.1404    
4          4          39         15         0.4081     0.1360    
4          8          39         13         0.3880     0.1293    
4          16         39         9          0.3552     0.1184    
8          4          28         13         0.3135     0.1045    
8          8          28         12         0.3036     0.1012    
8          16         28         7          0.2743     0.0914    
16         4          17         10         0.2199     0.0733    
16         8          17         10         0.2198     0.0733    
16         16         17         8          0.2108     0.0703    

TOTAL TIME: 5.9606076
```

#### Session 3

```
typeof(VECTOR)                          System.Numerics.Vector2
RyuJIT enabled                          False
SIMD enabled                            False
Iterations Per Test                     3000
Test Data Size                          966

RDP Error  Fit Error  Points     Curves     Time (s)   Time Per Iter (ms)
---------  ---------  ------     ------     --------   ------------------
1          4          77         14         2.5256     0.8419    
1          8          77         11         2.2484     0.7495    
1          16         77         10         2.1499     0.7166    
2          4          56         16         1.9074     0.6358    
2          8          56         11         1.6906     0.5635    
2          16         56         8          1.4908     0.4969    
4          4          39         15         1.3687     0.4562    
4          8          39         13         1.2984     0.4328    
4          16         39         9          1.1986     0.3995    
8          4          28         13         0.9588     0.3196    
8          8          28         12         0.9341     0.3114    
8          16         28         7          0.8352     0.2784    
16         4          17         10         0.5932     0.1977    
16         8          17         10         0.5932     0.1977    
16         16         17         8          0.5565     0.1855    

TOTAL TIME: 20.3493381

typeof(VECTOR)                          System.Numerics.Vector2
RyuJIT enabled                          True
SIMD enabled                            False
Iterations Per Test                     3000
Test Data Size                          966

RDP Error  Fit Error  Points     Curves     Time (s)   Time Per Iter (ms)
---------  ---------  ------     ------     --------   ------------------
1          4          77         14         2.4174     0.8058    
1          8          77         11         2.1394     0.7131    
1          16         77         10         2.0359     0.6786    
2          4          56         16         1.7973     0.5991    
2          8          56         11         1.5679     0.5226    
2          16         56         8          1.3385     0.4462    
4          4          39         15         1.2521     0.4174    
4          8          39         13         1.1772     0.3924    
4          16         39         9          1.0564     0.3521    
8          4          28         13         0.8373     0.2791    
8          8          28         12         0.8033     0.2678    
8          16         28         7          0.7035     0.2345    
16         4          17         10         0.4781     0.1594    
16         8          17         10         0.4779     0.1593    
16         16         17         8          0.4385     0.1462    

TOTAL TIME: 18.5206029

typeof(VECTOR)                          System.Numerics.Vector2
RyuJIT enabled                          True
SIMD enabled                            True
Iterations Per Test                     3000
Test Data Size                          966

RDP Error  Fit Error  Points     Curves     Time (s)   Time Per Iter (ms)
---------  ---------  ------     ------     --------   ------------------
1          4          77         14         0.6636     0.2212    
1          8          77         11         0.5988     0.1996    
1          16         77         10         0.5756     0.1919    
2          4          56         16         0.5318     0.1773    
2          8          56         11         0.4745     0.1582    
2          16         56         8          0.4211     0.1404    
4          4          39         15         0.4083     0.1361    
4          8          39         13         0.3883     0.1294    
4          16         39         9          0.3552     0.1184    
8          4          28         13         0.3129     0.1043    
8          8          28         12         0.3033     0.1011    
8          16         28         7          0.2736     0.0912    
16         4          17         10         0.2196     0.0732    
16         8          17         10         0.2198     0.0733    
16         16         17         8          0.2075     0.0692    

TOTAL TIME: 5.9536704
```
