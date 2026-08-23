#ifndef EROSION_SETTINGS
#define EROSION_SETTINGS

// The scale of the erosion effect, affecting it both horizontally and vertically.
float erosion_scale = 0.7;

// The strength of the erosion effect, affecting the magnitude of all octaves,
// and indirectly affecting the directions of the gullies as a result.
float erosion_strength = 0.22;

// The magnitude of the gullies as a weight value from 0 to 1.
// A value of 0 can sharpen peaks and valleys but feature virtually no gullies.
// A value of 1 produces full gullies but may leave peaks and valleys rounded.
// Adjusting erosion gully weight while inversely adjusting erosion scale can be
// used to control the sharpness of peaks and valleys while leaving gully
// magnitudes largely untocuhed.
float erosion_gully_weight = 0.5;

// The overall detail of the erosion. Lower values restrict the effect of higher
// frequency gullies to steeper slopes.
float erosion_detail = 1.5;

#define ridgeRounding 0.1
#define creaseRounding 0.5
// Separate rounding control of ridges and creases.
//  x: Rounding of ridges.
//  y: Rounding of creases.
//  z: Multiplier applied to the initial height function.
//     E.g. if the height function has noise of 5 times lower frequency
//     than the largest gullies, a value of 0.2 can compensate for that.
//  w: Multiplier applied to each subsequent gully octave after the first.
//     Setting it to the same value as the erosion lacunarity will produce
//     consistent rounding of all octaves.
float4 erosion_rounding = float4(ridgeRounding, creaseRounding, 0.1, 2.0);

// Control over how far away from ridges/creases the erosion takes effect.
//  x: Onset used on the initial height function.
//  y: Onset used on each gully octave.
//  z: RidgeMap-specific onset used on the initial height function.
//  w: RidgeMap-specific onset used on each gully octave.
float4 erosion_onset = float4(0.7, 1.25, 2.8, 1.5);

// Control over the assumed slope of the initial height function.
// In practise, assuming a slope can work better than using the input slope,
// since the final terrain can be shaped quite differently than the input.
//  x: An assumed slope value to override the actual slope.
//  y: The amount (from 0 to 1) to override the actual slope.
float2 erosion_assumed_slope = float2(0.7, 1.0);

// Gullies are based on stripes within Voronoi-like cells in the Phacelle noise
// function. The cell scale parameter controls the sizes of the cells relative
// to the overall erosion scale, while keeping the stripe widths unaffected.
// Values close to 1 usually produce good results. Smaller values produce more
// grainy gullies while larger values produce longer unbroken gullies, but too
// large values produce chaotic curved gullies that are not aligned with the
// slopes. Value changes can cause abrupt changes in output, especially far away
// from the origin, so this parameter is not well suited for animation or for
// modulation by other functions.
float erosion_cell_scale = 0.7;
// The degree of normalization applied in the Phacelle noise, between 0 and 1.
// The erosion filter depends on a certain consistency in magnitude of the
// Phacelle output. However, high values can create loopy results where ridges
// and creases meet up at a point, which produces unnatural looking results.
float erosion_normalization = 0.5;

// Control over the erosion octaves, with each successive octave layering
// smaller gullies onto the terrain.
int erosion_octaves = 5;
// The lacunarity controls the frequency (the inverse
// horizontal scale) of each octave relative to the last.
float erosion_lacunarity = 2.0;
// The gain controls the magnitude (the vertical
// scale) of each octave relative to the last.
float erosion_gain = 0.5;
#endif
