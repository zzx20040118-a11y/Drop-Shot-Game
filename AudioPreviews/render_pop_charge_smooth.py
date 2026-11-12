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
# Smooth the rising melody in log pitch; maintain one uninterrupted envelope.
knots = np.array([0, .07, .14, .21, .28, .35, .42, .485])
freq = np.exp(np.interp(t, knots, np.log(p['note_frequencies_hz'])))
phase = np.cumsum(freq) / sr
pulse = np.where(phase % 1 < p['pulse_width'], 1., -1.)
pulse -= 2 * p['pulse_width'] - 1
triangle = 2 / np.pi * np.arcsin(np.sin(2 * np.pi * phase))
core = p['pulse_gain'] * pulse + p['triangle_gain'] * triangle
core = sosfilt(butter(2, 6500, fs=sr, output='sos'), core)
# Quiet continuous upper harmonic retains sparkle without detached plucks.
bubble = np.sin(2 * np.pi * phase * 2) * (0.6 + 0.4 * t / .5)
y = core + p['bubble_gain'] * .55 * bubble
env = np.clip(t / .004, 0, 1) * np.clip((.5 - t) / .014, 0, 1)
y *= env * (.82 + .18 * t / .5)
y = np.tanh(y * 1.3)
y *= 10 ** (p['peak_dbfs'] / 20) / np.max(np.abs(y))
y[:96] *= np.linspace(0, 1, 96)
y[-96:] *= np.linspace(1, 0, 96)
wavfile.write(root / 'pop_charge_smooth_0.5s.wav', sr, np.round(y * 32767).astype(np.int16))
p.update(name='Pop Charge Smooth', pitch_mode='continuous log-frequency glide',
         pitch_knot_seconds=knots.tolist(), envelope_mode='single continuous envelope',
         description='Connected bubbly 8-bit rise with no gaps between notes')
(root / 'pop_charge_smooth.json').write_text(json.dumps(p, indent=2) + '\n')
assert len(y) == 24000 and np.isfinite(y).all()
print('Verified 0.500 seconds, peak %.2f dBFS' % (20 * np.log10(np.max(np.abs(y)))))
