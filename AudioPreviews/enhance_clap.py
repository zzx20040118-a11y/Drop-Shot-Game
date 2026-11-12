from pathlib import Path
import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, sosfilt

ROOT = Path(__file__).resolve().parent
sr, raw = wavfile.read(ROOT / 'clap_original.wav')
x = raw.astype(np.float64) / 32768
env = np.max(np.abs(x), axis=1)
active = np.flatnonzero(env > max(env.max() * 0.015, 0.0005))
start = max(0, int(active[0]) - int(sr * 0.001))
x = x[start:]
x /= max(np.max(np.abs(x)), 1e-9)
n = int(sr * 0.32)
t = np.arange(n) / sr
base = np.zeros((n, 2))
count = min(n, len(x))
base[:count] = x[:count]
# Keep the attack intact, shortening the tail for fast repeated hits.
base *= np.exp(-np.maximum(t - 0.055, 0) / 0.095)[:, None]
base *= np.minimum(1, (0.32 - t) / 0.015)[:, None]
body = sosfilt(butter(2, [160, 1300], fs=sr, btype='bandpass', output='sos'), base, axis=0)
rng = np.random.default_rng(41)
noise = sosfilt(butter(2, [1600, 7500], fs=sr, btype='bandpass', output='sos'), rng.normal(size=(n, 2)), axis=0)
noise *= ((1 - np.exp(-t / 0.0004)) * np.exp(-t / 0.017))[:, None]
thump = np.sin(2 * np.pi * (105 * t + 85 * 0.012 * (1 - np.exp(-t / 0.012))))
thump *= (1 - np.exp(-t / 0.0008)) * np.exp(-t / 0.026)

def write(name, y):
    y = np.tanh(y * 1.2) / np.tanh(1.2)
    y *= 10 ** (-1.5 / 20) / max(np.max(np.abs(y)), 1e-9)
    y[-int(sr * 0.008):] *= np.linspace(1, 0, int(sr * 0.008))[:, None]
    wavfile.write(ROOT / name, sr, np.round(y * 32767).astype(np.int16))
    print(name, 'duration', len(y) / sr, 'peak dBFS', round(20 * np.log10(np.max(np.abs(y))), 2), 'finite', bool(np.isfinite(y).all()))

write('clap_punchy.wav', base * 0.85 + body * 0.38 + thump[:, None] * 0.15 + noise * 0.065)
layered = base * 0.80 + body * 0.48 + thump[:, None] * 0.22 + noise * 0.11
for delay, gain in [(0.007, 0.15), (0.014, 0.09)]:
    d = int(sr * delay)
    layered[d:] += base[:-d] * gain
write('clap_perfect.wav', layered)
print('Removed leading samples:', start, 'milliseconds:', round(start / sr * 1000, 2))
