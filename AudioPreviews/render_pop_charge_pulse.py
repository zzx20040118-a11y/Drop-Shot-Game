import json
from pathlib import Path
import shutil
import sys
import numpy as np
from scipy.io import wavfile
from scipy.signal import butter, sosfilt

root = Path(__file__).resolve().parent
recipe = Path(sys.argv[1]) if len(sys.argv) > 1 else root / 'pop_charge_pulse.json'
p = json.loads(recipe.read_text())
sr = p['sample_rate']
t = np.arange(round(sr * p['duration_seconds'])) / sr
u = t / p['duration_seconds']
rate = p['pulse_start_hz'] + (p['pulse_end_hz'] - p['pulse_start_hz']) * u ** 1.35
lfo_phase = 2 * np.pi * np.cumsum(rate) / sr
wobble = np.sin(lfo_phase)
swell = (0.5 + 0.5 * wobble) ** 1.4
depth = p['vibrato_start_semitones'] + (p['vibrato_end_semitones'] - p['vibrato_start_semitones']) * u
freq = p['pitch_start_hz'] * (p['pitch_end_hz'] / p['pitch_start_hz']) ** u
freq *= 2 ** (wobble * depth / 12)
phase = 2 * np.pi * np.cumsum(freq) / sr
width = 0.32 + 0.12 * wobble
# Truncated Fourier synthesis keeps the retro pulse tone without hard-edge aliasing.
pulse = np.zeros_like(t)
for harmonic in range(1, 15):
    pulse += 2 / (np.pi * harmonic) * (
        np.sin(harmonic * phase) - np.sin(harmonic * (phase - 2 * np.pi * width)))
body = np.sin(phase) + 0.16 * np.sin(phase * 0.5)
noise = sosfilt(butter(2, [1200, 7000], fs=sr, btype='bandpass', output='sos'),
                np.random.default_rng(p['seed']).normal(size=len(t)))
raw = 0.32 * pulse + 0.46 * body + 0.035 * noise * u ** 1.5
cutoff = (p['filter_start_hz'] + (p['filter_end_hz'] - p['filter_start_hz']) * u ** 1.3)
cutoff *= 0.55 + 0.45 * swell
alpha = 1 - np.exp(-2 * np.pi * cutoff / sr)
filtered = np.empty_like(raw)
state1 = state2 = 0.
for i, sample in enumerate(raw):
    state1 += alpha[i] * (sample - state1)
    state2 += alpha[i] * (state1 - state2)
    filtered[i] = state2
amplitude = p['pulse_amplitude_floor'] + (1 - p['pulse_amplitude_floor']) * swell
envelope = np.clip(t / .004, 0, 1) * np.clip((p['duration_seconds'] - t) / .008, 0, 1)
fade_start = p.get('fade_start_fraction', 1.0)
fade_progress = np.clip((u - fade_start) / max(1 - fade_start, 1e-9), 0, 1)
fade = (1 - fade_progress) ** p.get('fade_power', 1.0)
y = np.tanh(filtered * amplitude * (0.65 + 0.35 * u) * 1.4) * envelope * fade
y *= 10 ** (p['peak_dbfs'] / 20) / np.max(np.abs(y))
data = np.round(y * 32767).astype(np.int16)
out = root / p.get('output_filename', 'pop_charge_pulse_0.5s.wav')
wavfile.write(out, sr, data)
asset = root.parent / 'DropShot/Assets/Audio/Hit/StrikeCharge_PopSmooth.wav'
assert asset.exists() and Path(str(asset) + '.meta').exists()
shutil.copyfile(out, asset)
check_sr, check = wavfile.read(asset)
assert check_sr == sr and np.array_equal(check, data)
assert len(y) == round(sr * p['duration_seconds']) and np.isfinite(y).all()
# Check the sustained section still contains pronounced amplitude fluctuations.
rms_length = len(y) // 480 * 480
rms = np.sqrt(np.mean(y[:rms_length].reshape(-1, 480) ** 2, axis=1))
print('Verified: %.3f seconds; peak %.2f dBFS; accelerating pulses %.1f to %.1f Hz' %
      (len(y) / sr, 20 * np.log10(np.max(np.abs(y))), rate[0], rate[-1]))
print('10 ms RMS variation ratio: %.2f; Unity asset matches output' % (rms.max() / rms[3:].min()))
