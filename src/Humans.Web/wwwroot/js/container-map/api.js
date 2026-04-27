// API calls for container placement.

export async function loadContainers(year) {
    const res = await fetch(`/api/city-planning/containers/${year}`);
    if (!res.ok) throw new Error(`Failed to load containers: ${res.status}`);
    return res.json();
}

export async function savePlacement(id, geoJson) {
    const res = await fetch(`/api/city-planning/containers/${id}/placement`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ geoJson }),
    });
    if (!res.ok) {
        const text = await res.text().catch(() => res.statusText);
        throw new Error(`Save failed (${res.status}): ${text}`);
    }
    return res.json();
}

export async function clearPlacement(id) {
    const res = await fetch(`/api/city-planning/containers/${id}/placement`, {
        method: 'DELETE',
    });
    if (!res.ok) {
        const text = await res.text().catch(() => res.statusText);
        throw new Error(`Clear failed (${res.status}): ${text}`);
    }
}
