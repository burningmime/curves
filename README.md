# curves

## Summary

This is a C# implementation of Philip J. Schneider's least-squares method for fitting Bézier  curves to a set of input data points, as well as several helper routines/classes to work with Bézier  curves.
It's designed to work with several different Vector types: WPF, the [SIMD-enabled vector types](http://www.nuget.org/packages/System.Numerics.Vectors) and [Unity 3D](http://unity3d.com/), and is
very simple to extend for use in other contexts. It's also high-performance and was profiled/micro-optimized to be quite fast without sacrificing API simplicity.

## Example

Say you have a bunch of input points like this (say, from a touchscreen or drawn by a mouse):

![readme-example-original.png](/images/readme-example-original.png?raw=true)

These are fairly smooth, but there's a bit of jitter, so you might want to smooth them out if you're drawing them. More importantly, these are very hard to work
with for computations. You need to represent this data in a format that's easy for a computer to work with. The answer is [Bézier  curves](http://en.wikipedia.org/wiki/B%C3%A9zier_curve),
but how do we get a curve that approximates the data? The first step (optional, but highly recommended) is to remove some of the excess points. The library supports two methods of doing this,
and the most popular method is the [Ramer-Douglas-Pueker algorithm](http://en.wikipedia.org/wiki/Ramer%E2%80%93Douglas%E2%80%93Peucker_algorithm). After doing this, the input data is reduced to this:

![readme-example-rdp.png](/images/readme-example-rdp.png?raw=true)

A lot nicer! For some programs, this is enough to work with. For others, we need to fit some curves to the data, which is when we call up our friend Philip J. Schneider:

![readme-example-fit.png](/images/readme-example-fit.png?raw=true)

The colors denote 3 separate Bézier curves. These form a [Composite Bézier curve](http://en.wikipedia.org/wiki/Composite_B%C3%A9zier_curve) that approximates the input data.
This "library" lets you do that in 2 lines of code:

```C#
List<Vector2> reduced = CurvePreprocess.RdpReduce(data, 2);   // use the Ramer-Douglas-Pueker algorithm to remove unnecessary points
CubicBézier[] curves = CurveFit.Fit(reduced, 8);              // fit the curves to those points
```

It also includes a WPF sample project so you can try this out for yourself and see what the parameters do, and how they affect the quality of the curves and the performance:

![readme-screenshot.png](/images/readme-screenshot.png?raw=true)

Neat, huh?

## See for yourself

TODO: link to a compiled binary, and maybe a video of the app in action?

## Using the code

This is not meant to be a library you compile into a DLL and link to. Because there are so many different Vector types flying around, it's easiest just to copy the source code
from the `trunk\burningmime.curves\src` folder directly into your project. You'll notice at the top of every file there, there's a very C-like construct:

```C#
#if SYSTEM_WINDOWS_VECTOR
using VECTOR = System.Windows.Vector;
using FLOAT = System.Double;
#elif SYSTEM_NUMERICS_VECTOR
using VECTOR = System.Numerics.Vector2;
using FLOAT = System.Single;
#elif UNITY
using VECTOR = UnityEngine.Vector2;
using FLOAT = System.Single;
#else
#error Unknown vector type -- must define one of SYSTEM_WINDOWS_VECTOR, SYSTEM_NUMERICS_VECTOR or UNITY
#endif
```

In your project properties, you can add one of the preprocessor symbols to the project depending on what you're targeting. Alternatively, you can simply do a `#define` at the top
of each file. In Unity you should add UNITY to the global custom defines in the project settings -- see http://docs.unity3d.com/Manual/PlatformDependentCompilation.html .

You can very easily add support for another vector type (assuming it has overloaded operators) by modifying `VectorHelper.cs`. For example, `SharpDX.Vector2` and
`Microsoft.Xna.Framework.Vector2` are trivial to add since they use the same interface as `System.Numerics.Vector2`. I haven't looked into WinRT much 
but it might have a vector type similar to `System.Windows.Vector` (WPF).

See the code documentation for usage info on the specific functions. The most important ones are the ones demonstrated above -- `CurvePreprocess.RdpReduce` to simplify input
data and `CurveFit.Fit` to fit curves to the data. You don't need to pre-process the input data, but it will fail if the input data contains repeated data points, so you should
at least call `CurvePreprocess.RemoveDuplicates` before doing the curve fit.

The parameters are tuneable based on your use case. I recommend playing around with the sample app for a bit to get a feel for exactly how the parameters and pre-processing methods
work.

TODO: document the spline, builder, etc here.

## Enabling SIMD

I'll just leave this here: http://www.drdobbs.com/windows/64-bit-simd-code-from-c/240168851

## Performance

Quite good indeed! I made a few sacrifices for API simplicity (most notably using lists instead of arrays for some things, which gave about a 2% slowdown in my tests), but generally
the performance is extremely good. It was tuned/profiled for the SIMD-enabled vector types and RyuJIT, so it might suffer a bit in other configurations, but hopefully not much.

TODO: some graphs, data, etc.

## Acknowledgments

This started out as a straight port from here: http://tog.acm.org/resources/GraphicsGems/gems/FitCurves.c

The Ramer-Douglas-Pueker code is an optimized version of the C# code here: http://www.codeproject.com/Articles/18936/A-Csharp-Implementation-of-Douglas-Peucker-Line-Ap