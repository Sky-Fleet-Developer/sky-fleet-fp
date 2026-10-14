#ifndef TERRAIN_VIEW
#define TERRAIN_VIEW

// TerrainViewMode, bound by TerrainProvider: 0 shaded, 1 biomes, 2 heightmap, 3 uv
float terrain_view_mode;

int TerrainViewMode()
{
    return (int)round(terrain_view_mode);
}

#endif
