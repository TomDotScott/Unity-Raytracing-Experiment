# My Unity Raytracing Experiment
I came across compute shaders watching a video by <a href="https://github.com/SebLague">Sebastian Lague</a> about Ray Marching, using compute shaders. In his video, he showcased that you could quite simply make a ray tracer using them, so I thought I could give it a go. This is my first foray into using them, and only my thrid time ever using Shaders in unity, with both previous times on my github, being the <a href="https://github.com/TomDotScott/Unity-Lightsabers">Lightsaber experiment</a> and my <a href="https://github.com/TomDotScott/Unity-Parkour">Unity game made for Coursework</a>

During this project I used several sources, mostly Reddit and Stackoverflow, as coding projects usually are, but this experiment has lead to me using academic articles more than I have in previous projects. This project is a Whitted ray tracer, using Lambert and Phone BRDFs. It is capable of tracing rays that intersect with spheres, planes, triangles and meshes. 
# Beauty Shots
## Albedos and Speculars
The sphere generation has several toggleable features, including the smoothness, albedo and speculars. My favourite of which is to have every sphere be specular. The glossy metal look is just very pleasing to me. I made a flying script to go on the camera, and this is one of my favourite images I took.

<img src="http://www.tomdotscott.com/images/Github/RayTracing/Raytracing1.png">

## Emission
On the second day of experimenting, I wanted to get emission working on the spheres. This was done by giving the light rays more energy as they reflected off the surface of spheres. I am really happy with how this turned out too.

<img src="http://www.tomdotscott.com/images/Github/RayTracing/Raytracing2.png">

Of course, one of the benefits of raytracing over any other rendering method is the photorealistic reflections and light bleeding. This next photo shows this off, with the colour bleeding on the floor and the specular spheres reflecting the emmissive ones as they would physically. 

<img src="http://www.tomdotscott.com/images/Github/RayTracing/Raytracing3.png">

## Meshes

The third day of the little experiment was spent reading up on how to intersect with triangles. I came across a paper from 1997 by Moller and Trumbore - http://fileadmin.cs.lth.se/cs/Personal/Tomas_Akenine-Moller/pubs/raytri_tam.pdf which showed C code for the optimal implementation of ray-triangle intersection, so that was very helpful :D

<img src="http://www.tomdotscott.com/images/Github/RayTracing/Raytracing4.png">

The ray tracer works with all of Unity's inbuilt 3-d shapes, and some very basic low-poly meshes. Here are some low-poly metal frogs sat in a circle because why not. 

<img src="http://www.tomdotscott.com/images/Github/RayTracing/Raytracing5.png">

# Conclusion
HLSL is scary at first, but is not too bad after spending a weekend of scratching my head over it. I would like to expand on this experiment in the future, taking into account textures on a model. I feel like the code can be optimised further, too, but doing so is above my ability level for now. On top of this, even after a long time rendering, the render is still quite noisy. This does clear up over time, but I imagine that there is probaby a simple fix to at least interpolate the noisy areas to smooth them out. In the meantime, thank you for checking out this little experiment and make sure to check out the rest of my github for others! 
