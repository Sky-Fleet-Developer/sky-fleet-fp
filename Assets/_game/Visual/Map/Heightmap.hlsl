#ifndef HEIGHTMAP
#define HEIGHTMAP

Texture2DArray<float> source_heightmap;
SamplerState sampler_source_heightmap;
float heightmap_shared_edges; // HeightmapSettings.sharedEdgePixels

#endif