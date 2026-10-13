#!/usr/bin/env python3
"""Label a confirmed trial's private local media without changing originals."""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re
import shutil


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build-dir', type=Path, required=True)
    parser.add_argument('--test-id', required=True)
    parser.add_argument('--media', type=Path, nargs='+', required=True)
    args = parser.parse_args()
    if not re.fullmatch(r'LMAR_v\d+\.\d+\.\d+_b\d+_a\d+', args.test_id):
        parser.error('Use the exact version/build/attempt ID displayed in the app.')
    build_id = args.test_id.rsplit('_a', 1)[0]
    manifest = args.build_dir / (build_id + '_build-manifest.json')
    if not manifest.is_file() or json.loads(manifest.read_text()).get('buildId') != build_id:
        parser.error('Test ID does not match this build manifest.')
    evidence = args.build_dir / 'Evidence'
    evidence.mkdir(exist_ok=True)
    index = evidence / 'evidence-map.json'
    records = json.loads(index.read_text()) if index.exists() else []
    for original in args.media:
        original = original.resolve(strict=True)
        if not original.is_file():
            parser.error('Media must be a local file.')
        name = re.sub(r'[^A-Za-z0-9_.-]', '_', original.name)
        destination = evidence / (args.test_id + '_' + name)
        if destination.exists():
            parser.error('Evidence filename already exists; no file was replaced.')
        try:
            os.link(original, destination)
        except OSError:
            shutil.copy2(original, destination)
        records.append({'testId': args.test_id, 'original': str(original),
                        'labeled': destination.name,
                        'registeredAtUtc': datetime.now(timezone.utc).isoformat()})
        temporary = index.with_suffix('.json.tmp')
        temporary.write_text(json.dumps(records, indent=2) + '\n')
        temporary.replace(index)
        print('Saved private labeled evidence:', destination.name)


if __name__ == '__main__':
    main()
