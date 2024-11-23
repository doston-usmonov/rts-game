import numpy as np
from scipy import signal
import soundfile as sf
import os
from concurrent.futures import ThreadPoolExecutor
from typing import Tuple, Optional
import json

# Configuration
CONFIG = {
    'sample_rate': 44100,
    'bit_depth': 16,
    'channels': 1,
    'output_dir': 'generated_sounds',
    'metadata': {
        'creator': 'RTS Game Audio Generator',
        'version': '1.0'
    }
}

class AudioGenerator:
    def __init__(self, sr: int = 44100):
        self.sr = sr
        self._ensure_output_dir()
        
    def _ensure_output_dir(self):
        if not os.path.exists(CONFIG['output_dir']):
            os.makedirs(CONFIG['output_dir'])
    
    @staticmethod
    def normalize_audio(audio: np.ndarray) -> np.ndarray:
        """Normalize audio to prevent clipping"""
        return audio / (np.max(np.abs(audio)) + 1e-6)
    
    @staticmethod
    def apply_fade(audio: np.ndarray, fade_duration: float = 0.1, sr: int = 44100) -> np.ndarray:
        """Apply fade in/out to prevent clicking"""
        fade_len = int(fade_duration * sr)
        fade_in = np.linspace(0, 1, fade_len)
        fade_out = np.linspace(1, 0, fade_len)
        
        audio[:fade_len] *= fade_in
        audio[-fade_len:] *= fade_out
        return audio
    
    def butter_filter(self, data: np.ndarray, cutoff: float, filter_type: str = 'low', order: int = 5) -> np.ndarray:
        nyq = 0.5 * self.sr
        normal_cutoff = cutoff / nyq
        b, a = signal.butter(order, normal_cutoff, btype=filter_type, analog=False)
        return signal.filtfilt(b, a, data)
    
    def create_rain_sound(self, duration: float = 5.0, intensity: float = 1.0) -> Tuple[np.ndarray, int]:
        """Create rain sound with variable intensity"""
        t = np.linspace(0, duration, int(self.sr * duration))
        noise = np.random.normal(0, intensity, len(t))
        
        # Rain drops
        drops = np.zeros_like(t)
        n_drops = int(duration * 100 * intensity)
        for _ in range(n_drops):
            drop_time = np.random.random() * duration
            drop_idx = int(drop_time * self.sr)
            if drop_idx < len(drops):
                drop_length = int(0.01 * self.sr)  # 10ms drops
                drop_env = np.exp(-np.linspace(0, 10, drop_length))
                end_idx = min(drop_idx + drop_length, len(drops))
                drops[drop_idx:end_idx] += drop_env[:end_idx-drop_idx] * np.random.random() * 0.5
        
        rain = noise * 0.3 + drops
        rain = self.butter_filter(rain, 8000)  # Remove very high frequencies
        rain = self.normalize_audio(rain)
        rain = self.apply_fade(rain)
        
        return rain, self.sr
    
    def create_wind_sound(self, duration: float = 5.0, intensity: float = 1.0) -> Tuple[np.ndarray, int]:
        """Create wind sound with variable intensity"""
        t = np.linspace(0, duration, int(self.sr * duration))
        
        # Base noise
        noise = np.random.normal(0, intensity, len(t))
        filtered_noise = self.butter_filter(noise, 1000)
        
        # Add wind gusts
        n_gusts = int(duration * 2 * intensity)
        gusts = np.zeros_like(t)
        for _ in range(n_gusts):
            gust_time = np.random.random() * duration
            gust_idx = int(gust_time * self.sr)
            if gust_idx < len(gusts):
                gust_length = int(0.5 * self.sr)  # 500ms gusts
                gust_env = np.sin(np.linspace(0, np.pi, gust_length)) * np.random.random()
                end_idx = min(gust_idx + gust_length, len(gusts))
                gusts[gust_idx:end_idx] += gust_env[:end_idx-gust_idx]
        
        wind = filtered_noise + gusts * intensity
        wind = self.normalize_audio(wind)
        wind = self.apply_fade(wind)
        
        return wind, self.sr
    
    def create_thunder_sound(self, duration: float = 3.0, intensity: float = 1.0) -> Tuple[np.ndarray, int]:
        """Create thunder sound with variable intensity"""
        t = np.linspace(0, duration, int(self.sr * duration))
        
        # Initial crack
        crack = np.random.normal(0, intensity, len(t))
        crack_env = np.zeros_like(t)
        crack_env[:int(0.1*self.sr)] = np.linspace(0, 1, int(0.1*self.sr))
        crack_env[int(0.1*self.sr):] = np.exp(-2 * (t[int(0.1*self.sr):] - 0.1))
        crack *= crack_env
        
        # Low rumble
        rumble = np.random.normal(0, intensity, len(t))
        rumble = self.butter_filter(rumble, 100)
        rumble *= np.exp(-0.5 * t)
        
        thunder = crack * 0.7 + rumble * 0.3
        thunder = self.normalize_audio(thunder)
        thunder = self.apply_fade(thunder, fade_duration=0.05)
        
        return thunder, self.sr
    
    def create_ground_sound(self, type: str = 'wet', duration: float = 1.0, intensity: float = 1.0) -> Tuple[np.ndarray, int]:
        """Create ground interaction sounds with variable intensity"""
        t = np.linspace(0, duration, int(self.sr * duration))
        noise = np.random.normal(0, intensity, len(t))
        
        sound_params = {
            'wet': {'cutoff': 1000, 'resonance': 5, 'decay': 5},
            'snow': {'cutoff': 3000, 'resonance': 2, 'decay': 3},
            'ice': {'cutoff': 4000, 'resonance': 8, 'decay': 8},
            'mud': {'cutoff': 500, 'resonance': 3, 'decay': 4}
        }
        
        params = sound_params.get(type, sound_params['wet'])
        filtered = self.butter_filter(noise, params['cutoff'])
        
        # Add resonance
        for _ in range(params['resonance']):
            filtered = self.butter_filter(filtered, params['cutoff'])
        
        # Apply envelope
        envelope = np.exp(-params['decay'] * t)
        sound = filtered * envelope * intensity
        
        sound = self.normalize_audio(sound)
        sound = self.apply_fade(sound)
        
        return sound, self.sr
    
    def save_sound(self, sound: np.ndarray, filename: str, sr: int, metadata: Optional[dict] = None) -> None:
        """Save sound with metadata"""
        filepath = os.path.join(CONFIG['output_dir'], filename)
        if metadata is None:
            metadata = {}
        
        # Combine with default metadata
        full_metadata = {**CONFIG['metadata'], **metadata}
        
        sf.write(filepath, sound, sr, subtype='PCM_16')
        
        # Save metadata
        metadata_path = f"{filepath}.json"
        with open(metadata_path, 'w') as f:
            json.dump(full_metadata, f, indent=2)

def generate_all_sounds():
    """Generate all game sounds with multiple variations"""
    generator = AudioGenerator(CONFIG['sample_rate'])
    
    def generate_variations(func, base_name, variations):
        for intensity in variations:
            sound, sr = func(intensity=intensity)
            metadata = {
                'type': base_name,
                'intensity': intensity,
                'duration': len(sound) / sr
            }
            filename = f"{base_name}_{int(intensity*100)}.wav"
            generator.save_sound(sound, filename, sr, metadata)
    
    # Define intensity variations
    variations = [0.3, 0.6, 1.0]
    
    with ThreadPoolExecutor() as executor:
        # Generate weather sounds
        executor.submit(generate_variations, generator.create_rain_sound, 'rain', variations)
        executor.submit(generate_variations, generator.create_wind_sound, 'wind', variations)
        executor.submit(generate_variations, generator.create_thunder_sound, 'thunder', variations)
        
        # Generate ground sounds
        for ground_type in ['wet', 'snow', 'ice', 'mud']:
            executor.submit(generate_variations, 
                          lambda intensity: generator.create_ground_sound(ground_type, intensity=intensity),
                          f'ground_{ground_type}',
                          variations)

if __name__ == '__main__':
    print("Starting sound generation...")
    generate_all_sounds()
    print("All sound files have been generated successfully!")
