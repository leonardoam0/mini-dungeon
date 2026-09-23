"""Compara capturas da cena de referência com quadros do vídeo de referência.

Uso:
    python Tools/compare_reference.py <pasta_capturas> <pasta_quadros_video> <pasta_saida> [video.mp4]

- Quadros do vídeo: f_001.png = 0,0 s, f_002.png = 0,5 s, ... (540x960, quadro 15·k do vídeo a 30 fps).
- Com o vídeo informado (requer OpenCV), cada captura é comparada com o quadro exato do instante.
- Capturas: ref_XX.Xs_9x16.png geradas por -reference -capture.
Gera, para cada instante, uma imagem lado a lado (vídeo | jogo | diferença) e um relatório
com indicadores simples por região (média de cor e luminância). Os números servem para orientar
a calibração; não são uma medida de "fidelidade".
"""
import os
import re
import sys

import numpy as np
from PIL import Image, ImageDraw

REGIONS = {
    # frações (x0, y0, x1, y1) do quadro 540x960
    "quadro_inteiro": (0.0, 0.0, 1.0, 1.0),
    "centro_jogo": (0.15, 0.25, 0.85, 0.75),
    "faixa_hud": (0.0, 0.86, 1.0, 1.0),
    "topo": (0.0, 0.0, 1.0, 0.2),
}


def luminance(a):
    return 0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]


def region(a, box):
    h, w = a.shape[:2]
    x0, y0, x1, y1 = box
    return a[int(y0 * h):int(y1 * h), int(x0 * w):int(x1 * w)]


def coarse_correlation(a, b, size=(27, 48)):
    """Correlação das luminâncias reduzidas: indica se as massas claras/escuras coincidem."""
    la = np.asarray(Image.fromarray(a).convert("L").resize(size, Image.BILINEAR), dtype=np.float64).ravel()
    lb = np.asarray(Image.fromarray(b).convert("L").resize(size, Image.BILINEAR), dtype=np.float64).ravel()
    la -= la.mean()
    lb -= lb.mean()
    d = np.sqrt((la * la).sum() * (lb * lb).sum())
    return float((la * lb).sum() / d) if d > 0 else 0.0


def main():
    if len(sys.argv) < 4:
        print(__doc__)
        return 1
    cap_dir, ref_dir, out_dir = sys.argv[1:4]
    video_frames = None
    if len(sys.argv) > 4:
        import cv2
        vc = cv2.VideoCapture(sys.argv[4])
        fps = vc.get(cv2.CAP_PROP_FPS) or 30.0
        video_frames = []
        while True:
            ok, fr = vc.read()
            if not ok:
                break
            video_frames.append(cv2.cvtColor(fr, cv2.COLOR_BGR2RGB))
    os.makedirs(out_dir, exist_ok=True)
    pat = re.compile(r"ref_(\d+\.\d)s_9x16\.png$")
    rows = []
    for name in sorted(os.listdir(cap_dir)):
        m = pat.match(name)
        if not m:
            continue
        t = float(m.group(1))
        if video_frames:
            ref = Image.fromarray(video_frames[min(len(video_frames) - 1, int(round(t * fps)))])
        else:
            idx = int(round(t / 0.5)) + 1
            ref_path = os.path.join(ref_dir, f"f_{idx:03d}.png")
            if not os.path.exists(ref_path):
                print(f"sem quadro de vídeo para {t:.1f}s ({ref_path})")
                continue
            ref = Image.open(ref_path).convert("RGB")
        game = Image.open(os.path.join(cap_dir, name)).convert("RGB")
        if game.size != ref.size:
            game = game.resize(ref.size, Image.LANCZOS)
        ga = np.asarray(game)
        ra = np.asarray(ref)
        diff = np.abs(ga.astype(np.int16) - ra.astype(np.int16)).astype(np.uint8)
        w, h = ref.size
        sheet = Image.new("RGB", (w * 3 + 20, h + 40), (12, 12, 12))
        sheet.paste(ref, (0, 40))
        sheet.paste(game, (w + 10, 40))
        sheet.paste(Image.fromarray(np.clip(diff.astype(np.int16) * 2, 0, 255).astype(np.uint8)), (w * 2 + 20, 40))
        d = ImageDraw.Draw(sheet)
        d.text((8, 10), f"video {t:.1f}s", fill=(255, 255, 255))
        d.text((w + 18, 10), f"jogo {t:.1f}s", fill=(255, 255, 255))
        d.text((w * 2 + 28, 10), "diferenca x2", fill=(255, 255, 255))
        sheet.save(os.path.join(out_dir, f"cmp_{t:04.1f}s.png"))

        stats = {"t": t, "corr": coarse_correlation(ga, ra)}
        for key, box in REGIONS.items():
            g = region(ga, box).astype(np.float64)
            r = region(ra, box).astype(np.float64)
            stats[key] = {
                "rgb_jogo": g.reshape(-1, 3).mean(0),
                "rgb_video": r.reshape(-1, 3).mean(0),
                "lum_jogo": luminance(g).mean(),
                "lum_video": luminance(r).mean(),
            }
        rows.append(stats)

    lines = ["# Comparação por instante (indicadores de calibração, não medida de fidelidade)", ""]

    # Enquadramento: topo do obelisco no vídeo (marcos conferidos) × projeção exata registrada na captura.
    marks_path = os.path.join(ref_dir, "..", "marcos_video.json")
    log_path = os.path.join(cap_dir, "player.log")
    if os.path.exists(marks_path) and os.path.exists(log_path):
        import json
        marks = json.load(open(marks_path, encoding="utf-8"))["obelisco_topo"]
        game = {}
        for line in open(log_path, encoding="utf-8", errors="ignore"):
            m2 = re.search(r"\[Captura\] ref_(\d+\.\d)s jogador_local=\S+ obelisco_topo_9x16=([-\d.]+);([-\d.]+)", line)
            if m2:
                game[float(m2.group(1))] = (float(m2.group(2)), float(m2.group(3)))
        lines += ["## Enquadramento — topo do obelisco", "",
                  "| instante | vídeo (x, y) | jogo (x, y) | desvio (px) | % da altura (960) |", "| --- | --- | --- | --- | --- |"]
        errs = []
        for key in sorted(marks, key=float):
            t = float(key)
            if t not in game:
                continue
            vx, vy = marks[key]
            gx, gy = game[t]
            d = ((gx - vx) ** 2 + (gy - vy) ** 2) ** 0.5
            errs.append(d / 9.6)
            lines.append(f"| {t:.1f} s | ({vx:.0f}, {vy:.0f}) | ({gx:.0f}, {gy:.0f}) | {gx - vx:+.0f}, {gy - vy:+.0f} | {d / 9.6:.1f}% |")
        if errs:
            lines += ["", f"Mediana do desvio: {sorted(errs)[len(errs) // 2]:.1f}% da altura do quadro; máximo {max(errs):.1f}%.", ""]
    for s in rows:
        lines.append(f"## {s['t']:.1f} s  (correlação de massas claras/escuras: {s['corr']:+.2f})")
        for key in REGIONS:
            v = s[key]
            gj = ", ".join(f"{x:5.1f}" for x in v["rgb_jogo"])
            rv = ", ".join(f"{x:5.1f}" for x in v["rgb_video"])
            lines.append(f"- {key}: luminância jogo {v['lum_jogo']:5.1f} × vídeo {v['lum_video']:5.1f} | RGB jogo ({gj}) × vídeo ({rv})")
        lines.append("")
    report = "\n".join(lines)
    with open(os.path.join(out_dir, "comparacao.md"), "w", encoding="utf-8") as f:
        f.write(report)
    print(report)
    return 0


if __name__ == "__main__":
    sys.exit(main())
