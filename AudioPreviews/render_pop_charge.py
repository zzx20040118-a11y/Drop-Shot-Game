import json
from pathlib import Path
import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, sosfilt

root = Path(__file__).resolve().parent
p = json.loads((root / 'pop_charge.json').read_text())
sr = p['sample_rate']
n = round(sr * p['duration_seconds'])
t = np.arange(n) / sr
edges = np.array(p['note_boundaries_seconds'])
idx = np.clip(np.searchsorted(edges, t, side='right') - 1, 0, 7)
local = t - edges[idx]
length = edges[idx + 1] - edges[idx]
# A small upward bend adds a playful bubble character to each step.
freq = np.array(p['note_frequencies_hz'])[idx] * (1 + 0.045 * (1 - np.exp(-local / 0.018)))
phase = np.cumsum(freq) / sr
pulse = np.where(phase % 1 < p['pulse_width'], 1., -1.)
pulse -= 2 * p['pulse_width'] - 1
triangle = 2 / np.pi * np.arcsin(np.sin(2 * np.pi * phase))
note_env = np.minimum(local / 0.0025, 1) * np.minimum((length - local) / 0.005, 1)
note_env = np.clip(note_env, 0, 1) * (0.72 + 0.28 * np.exp(-local / 0.025))
core = (p['pulse_gain'] * pulse + p['triangle_gain'] * triangle) * note_env
core = sosfilt(butter(2, 6500, fs=sr, output='sos'), core)
bubble_phase = np.cumsum(freq * (1.8 + 0.7 * np.exp(-local / 0.009))) / sr
bubble = np.sin(2 * np.pi * bubble_phase) * np.exp(-local / 0.018) * np.minimum(local / 0.0015, 1)
y = core + p['bubble_gain'] * bubble
global_env = np.minimum(t / 0.003, 1) * np.minimum((0.5 - t) / 0.014, 1)
y *= np.clip(global_env, 0, 1)
y -= y.mean()
y = np.tanh(y * 1.3)
y *= 10 ** (p['peak_dbfs'] / 20) / np.max(np.abs(y))
y[:96] *= np.linspace(0, 1, 96)
y[-96:] *= np.linspace(1, 0, 96)
wavfile.write(root / 'pop_charge_0.5s.wav', sr, np.round(y * 32767).astype(np.int16))
assert len(y) == 24000 and np.isfinite(y).all()
print('Verified: 0.500 seconds, 48 kHz, peak %.2f dBFS' % (20 * np.log10(np.max(np.abs(y)))))
