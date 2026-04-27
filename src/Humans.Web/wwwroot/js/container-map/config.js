// Server-side values injected via data-* attributes on #map.
const el = document.getElementById('map');

export const CONFIG = {
    YEAR:                parseInt(el.dataset.year, 10),
    IS_MAP_ADMIN:        el.dataset.isMapAdmin === 'true',
    USER_CAMP_SEASON_ID: el.dataset.userCampSeasonId || null,

    ESRI_TILES: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
    MAP_BOUNDS: [
        [-0.14285979741055144, 41.696961407716145],
        [-0.13157837273621453, 41.70290716137069],
    ],
};
