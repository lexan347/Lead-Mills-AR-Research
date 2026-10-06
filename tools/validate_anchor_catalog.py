#!/usr/bin/env python3
"""Validate the project anchor catalog without third-party dependencies."""

from __future__ import annotations

import json
import sys
from pathlib import Path
from typing import Any


ALLOWED_STATUSES = {"draft", "surveyed", "validated", "retired"}
ALLOWED_ALTITUDE_REFERENCES = {
    "unresolved",
    "WGS84-ellipsoid",
    "orthometric",
    "terrain-relative",
    "local-site-frame",
}


def validate_catalog(data: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    if data.get("schemaVersion") != 1:
        errors.append("schemaVersion must equal 1")
    if data.get("coordinateReference") != "WGS84":
        errors.append("coordinateReference must equal WGS84")
    if not isinstance(data.get("catalogId"), str) or not data["catalogId"].strip():
        errors.append("catalogId must be a non-empty string")

    anchors = data.get("anchors")
    if not isinstance(anchors, list):
        return errors + ["anchors must be an array"]

    seen: set[str] = set()
    for index, anchor in enumerate(anchors):
        prefix = f"anchors[{index}]"
        if not isinstance(anchor, dict):
            errors.append(f"{prefix} must be an object")
            continue

        anchor_id = anchor.get("id")
        if not isinstance(anchor_id, str) or not anchor_id:
            errors.append(f"{prefix}.id must be a non-empty string")
        elif anchor_id in seen:
            errors.append(f"{prefix}.id is duplicated: {anchor_id}")
        else:
            seen.add(anchor_id)

        lat, lon = anchor.get("latitude"), anchor.get("longitude")
        if (lat is None) != (lon is None):
            errors.append(f"{prefix} latitude and longitude must both be null or both be numbers")
        if lat is not None and (not isinstance(lat, (int, float)) or not -90 <= lat <= 90):
            errors.append(f"{prefix}.latitude must be between -90 and 90")
        if lon is not None and (not isinstance(lon, (int, float)) or not -180 <= lon <= 180):
            errors.append(f"{prefix}.longitude must be between -180 and 180")

        heading = anchor.get("headingDegreesTrue")
        if heading is not None and (
            not isinstance(heading, (int, float)) or not 0 <= heading < 360
        ):
            errors.append(f"{prefix}.headingDegreesTrue must be in [0, 360)")

        if anchor.get("status") not in ALLOWED_STATUSES:
            errors.append(f"{prefix}.status is not recognized")
        if anchor.get("altitudeReference") not in ALLOWED_ALTITUDE_REFERENCES:
            errors.append(f"{prefix}.altitudeReference is not recognized")
        if not isinstance(anchor.get("publicRelease"), bool):
            errors.append(f"{prefix}.publicRelease must be boolean")

        if anchor.get("publicRelease") and (lat is None or lon is None):
            errors.append(f"{prefix} cannot be publicRelease without coordinates")
        if anchor.get("status") in {"surveyed", "validated"} and lat is None:
            errors.append(f"{prefix} is {anchor.get('status')} but has no coordinates")

        for field in (
            "horizontalUncertaintyMeters",
            "verticalUncertaintyMeters",
            "headingUncertaintyDegrees",
        ):
            value = anchor.get(field)
            if value is not None and (
                not isinstance(value, (int, float)) or value < 0
            ):
                errors.append(f"{prefix}.{field} must be null or nonnegative")

    return errors


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: validate_anchor_catalog.py PATH", file=sys.stderr)
        return 2
    path = Path(sys.argv[1])
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1
    errors = validate_catalog(data)
    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1
    print(f"OK: {path} contains {len(data['anchors'])} anchor(s)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
