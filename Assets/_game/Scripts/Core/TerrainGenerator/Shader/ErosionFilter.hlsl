#pragma once

#define TAU 6.28318530717959

// -----------------------------------------------------------------------------
// Misc utility functions
// -----------------------------------------------------------------------------

#define DEG_TO_RAD (PI / 180.0)
#define clamp01(x) clamp(x, 0.0, 1.0)
#define sq(x) (x*x)

float2 hash(in float2 x) {
    const float2 k = float2(0.3183099, 0.3678794);
    x = x * k + k.yx;
    return -1.0 + 2.0 * frac(16.0 * k * frac(x.x * x.y * (x.x + x.y)));
}


// The Simple Phacelle Noise function produces a stripe pattern aligned with the input vector.
// The name Phacelle is a portmanteau of phase and cell, since the function produces a phase by
// interpolating cosine and sine waves from multiple cells.
//  - p is the input point being evaluated.
//  - normDir is the direction of the stripes at this point. It must be a normalized vector.
//  - freq is the freqency of the stripes within each cell. It's best to keep it close to 1.0, as
//    high values will produce distortions and other artifacts.
//  - offset is the phase offset of the stripes, where 1.0 is a full cycle.
//  - normalization is the degree of normalization applied, between 0 and 1. With e.g. a value of
//    0.4, raw output with a magnitude below 0.6 won't get fully normalized to a magnitude of 1.0.
// Phacelle Noise function copyright (c) 2025 Rune Skovbo Johansen
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
float4 PhacelleNoise(in float2 p, float2 normDir, float freq, float offset, float normalization) {
    // Get a vector orthogonal to the input direction, with a
    // magnitude proportional to the frequency of the stripes.
    float2 sideDir = normDir.yx * float2(-1.0, 1.0) * freq * TAU;
    offset *= TAU;

    // Iterate over 4x4 cells, calculating a stripe pattern for each and blending between them.
    // pInt is the integer part of the current coordinate p, pFrac is the remainder.
    //
    // o   o   o   o
    //
    // o   o   o   o
    //       p
    // o   i   o   o
    //
    // o   o   o   o
    //
    // p: current coordinate    i: integer part of p    o: grid points for 4x4 cells
    //
    float2 pInt = floor(p);
    float2 pFrac = frac(p);
    float2 phaseDir = float2(0, 0);
    float weightSum = 0.0;
    for (int i = -1; i <= 2; i++) {
        for (int j = -1; j <= 2; j++) {
            float2 gridOffset = float2(i, j);

            // Calculate a cell point by starting off with a point in the integer grid.
            float2 gridPoint = pInt + gridOffset;

            // Calculate a random offset for the cell point between -0.5 and 0.5 on each axis.
            float2 randomOffset = hash(gridPoint) * 0.5;

            // The final cell point (we don't store it) is the gridPoint plus the randomOffset.
            // Calculate a vector representing the input point relative to this cell point:
            // p - (gridPoint + randomOffset)
            // = (pFrac + pInt) - ((pInt + gridOffset) + randomOffset)
            // = pFrac + pInt - pInt - gridOffset - randomOffset
            // = pFrac - gridOffset - randomOffset
            float2 vectorFromCellPoint = pFrac - gridOffset - randomOffset;

            // Bell-shaped weight function which is 1 at dist 0 and nearly 0 at dist 1.5.
            // Due to the random offsets of up to 0.5, the closest a cell point not in the 4x4
            // grid can be to the current point p is 1.5 units away.
            float sqrDist = dot(vectorFromCellPoint, vectorFromCellPoint);
            float weight = exp(-sqrDist * 2.0);
            // Subtract 0.01111 to make the function actually 0 at distance 1.5, which avoids
            // some (very subtle) grid line artefacts.
            weight = max(0.0, weight - 0.01111);

            // Keep track of the total sum of weights.
            weightSum += weight;

            // The waveInput is a gradient which increases in value along sideDir. Its rate of
            // change is the freq times tau, due to the multiplier pre-applied to sideDir.
            float waveInput = dot(vectorFromCellPoint, sideDir) + offset;

            // Add this cell's cosine and sine wave contributions to the interpolated value.
            phaseDir += float2(cos(waveInput), sin(waveInput)) * weight;
        }
    }

    // Get the raw interpolated value.
    float2 interpolated = phaseDir / weightSum;
    // Interpret the value as a vector whose length represents the magnitude of both waves.
    float magnitude = sqrt(dot(interpolated, interpolated));
    // Apply a lower threshold to show small magnitudes we're going to fully normalize.
    magnitude = max(1.0 - normalization, magnitude);
    // Return a vector containing the normalized cosine and sine waves, as well as the direction
    // vector, which can be multiplied onto the sine to get the derivatives of the cosine.
    return float4(interpolated / magnitude, sideDir);
}

// -----------------------------------------------------------------------------
// EROSION FUNCTION
// -----------------------------------------------------------------------------

// First a few utility functions.

float pow_inv(float t, float power) {
    // Flip, raise to the specified power, and flip back.
    return 1.0 - pow(1.0 - clamp01(t), power);
}

float ease_out(float t) {
    // Flip by subtracting from one.
    float v = 1.0 - clamp01(t);
    // Raise to a power of two and flip back.
    return 1.0 - v * v;
}

float smooth_start(float t, float smoothing) {
    if (t >= smoothing)
        return t - 0.5 * smoothing;
    return 0.5 * t * t / smoothing;
}

float2 safe_normalize(float2 n) {
 	// A div-by-zero-safe replacement for normalize.
    float l = length(n);
	return (abs(l) > 1e-10) ? (n / l) : n;	
}

// Advanced Terrain Erosion Filter copyright (c) 2025 Rune Skovbo Johansen
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.
float4 ErosionFilter(
    // Input parameters that vary per pixel.
    in float2 p, float3 heightAndSlope, float fadeTarget,
    // Stylistic parameters that may vary per pixel.
    float strength, float gullyWeight, float detail, float4 rounding, float4 onset, float2 assumedSlope,
    // Scale related parameters that do not support variation per pixel.
    float scale, int octaves, float lacunarity,
    // Other parameters.
    float gain, float cellScale, float normalization,
    // Output parameters.
    out float ridgeMap, out float debug
) {
    strength *= scale;
    fadeTarget = clamp(fadeTarget, -1.0, 1.0);
    
    float3 inputHeightAndSlope = heightAndSlope;
    float freq = 1.0 / (scale * cellScale);
    float slopeLength = max(length(heightAndSlope.yz), 1e-10);
    float magnitude = 0.0;
    float roundingMult = 1.0;
    
    float roundingForInput = lerp(rounding.y, rounding.x, clamp01(fadeTarget + 0.5)) * rounding.z;
    // The combined accumulating mask, based first on initial slope, and later on slope of each octave too.
    float combiMask = ease_out(smooth_start(slopeLength * onset.x, roundingForInput * onset.x));

    // Initialize the ridgeMap fadeTarget and mask.
    float ridgeMapCombiMask = ease_out(slopeLength * onset.z);
    float ridgeMapFadeTarget = fadeTarget;
    
    // Deteriming the strength of the initial slope used for gully directions
    // based on the specified mix of the actual slope and an assumed slope.
    float2 gullySlope = lerp(heightAndSlope.yz, heightAndSlope.yz / slopeLength * assumedSlope.x, assumedSlope.y);
    
    for (int i = 0; i < octaves; i++) {
        // Calculate and add gullies to the height and slope.
        float4 phacelle = PhacelleNoise(p * freq, safe_normalize(gullySlope), cellScale, 0.25, normalization);
        // Multiply with freq since p was multiplied with freq.
        // Negate since we use slope directions that point down.
        phacelle.zw *= -freq;
        // Amount of slope as value from 0 to 1.
        float sloping = abs(phacelle.y);
        
        // Add non-masked, normalized slope to gullySlope, for use by subsequent octaves.
        // It's normalized to use the steepest part of the sine wave everywhere.
        gullySlope += sign(phacelle.y) * phacelle.zw * strength * gullyWeight;
        
        // Handle height offset and approximate output slope.
        
        // Gullies has height offset (from -1 to 1) in x and derivative in yz.
        float3 gullies = float3(phacelle.x, phacelle.y * phacelle.zw);
        // Fade gullies towards fadeTarget based on combiMask.
        float3 fadedGullies = lerp(float3(fadeTarget, 0.0, 0.0), gullies * gullyWeight, combiMask);
        // Apply height offset and derivative (slope) according to strength of current octave.
        heightAndSlope += fadedGullies * strength;
        magnitude += strength;
        
        // Update fadeTarget to include the new octave.
        fadeTarget = fadedGullies.x;
        
        // Update the mask to include the new octave.
        float roundingForOctave = lerp(rounding.y, rounding.x, clamp01(phacelle.x + 0.5)) * roundingMult;
        float newMask = ease_out(smooth_start(sloping * onset.y, roundingForOctave * onset.y));
        combiMask = pow_inv(combiMask, detail) * newMask;
        
        // Update the ridgeMap fadeTarget and mask.
        ridgeMapFadeTarget = lerp(ridgeMapFadeTarget, gullies.x, ridgeMapCombiMask);
        float newRidgeMapMask = ease_out(sloping * onset.w);
        ridgeMapCombiMask = ridgeMapCombiMask * newRidgeMapMask;

        // Prepare the next octave.
        strength *= gain;
        freq *= lacunarity;
        roundingMult *= rounding.w;
    }
    
    ridgeMap = ridgeMapFadeTarget * (1.0 - ridgeMapCombiMask);
    debug = fadeTarget;
    
    float3 heightAndSlopeDelta = heightAndSlope - inputHeightAndSlope;
    return float4(heightAndSlopeDelta, magnitude);
}
