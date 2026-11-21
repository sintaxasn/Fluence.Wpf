"""Generate the independently authored 256x256 acrylic noise tile.

The xorshift32 sequence starts at 0xA53C9E17. The rounded average of its two
low bytes gives each opaque pixel a grayscale value. Contrast is reduced
to seven-eighths around gray 128 to match the composited grain. The renderer
applies node opacity .02.
Only Python's standard library is needed; output bytes are reproducible.
"""

import hashlib
import json
import pathlib
import statistics
import struct
import zlib


SIZE = 256
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
            gray = ((state & 0xFF) + ((state >> 8) & 0xFF) + 1) // 2
            value = (7 * gray + 128 + 4) // 8
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
        "algorithm": "xorshift32-soft-grayscale-v1",
        "seed": f"0x{SEED:08X}",
        "width": SIZE,
        "height": SIZE,
        "pixelFormat": "RGBA8",
        "rgb": "grayscale, rounded byte average with seven-eighths contrast around 128",
        "alpha": "255 at every pixel",
        "nodeOpacity": 0.02,
        "meanGray": sum(values) / len(values),
        "standardDeviationGray": statistics.pstdev(values),
        "minGray": min(values),
        "maxGray": max(values),
        "sha256": hashlib.sha256(png).hexdigest().upper(),
    }
    output.with_name("AcrylicNoise.json").write_bytes(
        (json.dumps(manifest, indent=2) + "\n").encode("utf-8")
    )


if __name__ == "__main__":
    main()
