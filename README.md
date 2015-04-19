# curves

### Summary

This is a C# implementation of Philip J. Schneider's least-squares method for fitting Bézier  curves to a set of input data points, as well as several helper routines/classes to work with Bézier  curves.
It's designed to work with several different Vector types: WPF, the [SIMD-enabled vector types](http://www.nuget.org/packages/System.Numerics.Vectors) and [Unity 3D](http://unity3d.com/), and is
very simple to extend for use in other contexts. It's also high-performance and was profiled/micro-optimized to be quite fast without sacrificing API simplicity.

### Example

Say you have a bunch of input points like this (ie from a touchscreen or drawn by a mouse):

![readme-example-original.png](/images/readme-example-original.png?raw=true)

This seems to form a an "S" shape. However, there's a bit of jitter visible, so if you're displaying it you might want to smooth it out.
More importantly, these points aren't evenly spaced and are generally quite difficult to work with programmatically. We need to transform this
data into a format a computer can easily work with. The answer is [Bézier  curves](http://en.wikipedia.org/wiki/B%C3%A9zier_curve).

How do we get a curve that approximates the data? The first step (optional, but highly recommended) is to remove some of the excess points. The most common method of doing this 
is the [Ramer-Douglas-Pueker algorithm](http://en.wikipedia.org/wiki/Ramer%E2%80%93Douglas%E2%80%93Peucker_algorithm). After running it, the input data looks like:

![readme-example-rdp.png](/images/readme-example-rdp.png?raw=true)

A lot nicer! For some programs, this is enough to work with. For others, we need to fit some curves to the data, which is when we call up our friend Philip J. Schneider:

![readme-example-fit.png](/images/readme-example-fit.png?raw=true)

The colors denote 3 separate Bézier curves. These form a [Composite Bézier curve](http://en.wikipedia.org/wiki/Composite_B%C3%A9zier_curve) with C1 continuity that approximates the input data.
This library lets you do that in 2 lines of code:

```C#
List<Vector2> reduced = CurvePreprocess.RdpReduce(data, 2);   // use the Ramer-Douglas-Pueker algorithm to remove unnecessary points
CubicBezier[] curves = CurveFit.Fit(reduced, 8);              // fit the curves to those points
```

It also includes a WPF sample project so you can try this out for yourself and see what the parameters do, and how they affect the quality of the curves and the performance:

![readme-screenshot.png](/images/readme-screenshot.png?raw=true)

Neat, huh?

### See for yourself

TODO: link to a compiled binary, and maybe a video of the app in action?

### Getting the code to work with your project

This is not meant to be a library you compile into a DLL and link to (if you're using C#). Because there are so many different Vector types flying around, it's easiest just to copy the source code
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

If you're using VB.Net, F#, or another .NET language, you'll need to compile it with the correct Vector type and reference the compiled DLL.

You can very easily add support for new vector types (assuming it has overloaded operators) by modifying [VectorHelper.cs](/burningmime.curves/src/VectorHelper.cs). For example, SharpDX.Vector2 and
Microsoft.Xna.Framework.Vector2 are trivial to add since they use the same interface as System.Numerics.Vector2. I haven't looked into WinRT much 
but it might have a vector type similar to System.Windows.Vector (WPF).

### Using the code

See the code documentation for usage info on the specific functions. The most important ones are the ones demonstrated above -- `CurvePreprocess.RdpReduce` to simplify input
data and `CurveFit.Fit` to fit curves to the data. You don't need to pre-process the input data, but it will fail if the input data contains repeated data points, so you must
at least call `CurvePreprocess.RemoveDuplicates` before doing the curve fit if you suspect the input data might have duplicates.

The parameters are tuneable based on your use case. I recommend playing around with the sample app for a bit to get a feel for exactly how the parameters and pre-processing methods
work. This will give a much better explanation than I could.

Included is also a CurveBuilder class which lets you incrementally add points and update curves as they come in. It uses its own pre-processing method (ignoring all points that
are too close the previous one, basically) and splits the final curve when it no longer fits the input data. This is useful if you want to build curves "as you go" rather than fitting
all at once, but isn't suitable for displaying to the user without some massaging first since it's still a bit jumpy.

TODO document SPline

### Enabling SIMD

I'll just leave this here: http://www.drdobbs.com/windows/64-bit-simd-code-from-c/240168851

### Acknowledgments

This started out as a straight port from here: http://tog.acm.org/resources/GraphicsGems/gems/FitCurves.c

The Ramer-Douglas-Pueker code is an optimized version of the C# code here: http://www.codeproject.com/Articles/18936/A-Csharp-Implementation-of-Douglas-Peucker-Line-Ap