"""Generate the independently authored 128x128 acrylic noise tile.

The xorshift32 sequence starts at 0xA53C9E17. Each state contributes its low
byte as grayscale RGB, with alpha 255. The renderer applies node opacity .02.
Only Python's standard library is needed; output bytes are reproducible.
"""

import hashlib
import json
import pathlib
import struct
import zlib


SIZE = 128
SEED = 0xA53C9E17


def chunk(tag: bytes, body: bytes) -> bytes:
    payload = tag + body
    return struct.pack(">I", len(body)) + payload + struct.pack(">I", zlib.crc32(payload) & 0xFFFFFFFF)


def main() -> None:
    state = SEED
    values = []
    rows = []
    for _ in range(SIZE):
        row = bytearray(b"\x00")
        for _ in range(SIZE):
            state ^= (state << 13) & 0xFFFFFFFF
            state ^= state >> 17
            state ^= (state << 5) & 0xFFFFFFFF
            value = state & 0xFF
            values.append(value)
            row.extend((value, value, value, 255))
        rows.append(row)
    png = (
        b"\x89PNG\r\n\x1a\n"
        + chunk(b"IHDR", struct.pack(">IIBBBBB", SIZE, SIZE, 8, 6, 0, 0, 0))
        + chunk(b"IDAT", zlib.compress(b"".join(rows), level=9))
        + chunk(b"IEND", b"")
    )
    output = pathlib.Path(__file__).with_name("AcrylicNoise.png")
    output.write_bytes(png)
    manifest = {
        "algorithm": "xorshift32-low-byte-v1",
        "seed": f"0x{SEED:08X}",
        "width": SIZE,
        "height": SIZE,
        "pixelFormat": "RGBA8",
        "rgb": "grayscale, one xorshift32 low byte per pixel",
        "alpha": "255 at every pixel",
        "nodeOpacity": 0.02,
        "meanGray": sum(values) / len(values),
        "minGray": min(values),
        "maxGray": max(values),
        "sha256": hashlib.sha256(png).hexdigest().upper(),
    }
    output.with_name("AcrylicNoise.json").write_text(
        json.dumps(manifest, indent=2) + "\n", encoding="utf-8"
    )


if __name__ == "__main__":
    main()
