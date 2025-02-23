using UnityEngine;
using System;

public static class AudioUtility
{
    public static AudioClip ConvertByteArrayToAudioClip(byte[] rawData)
    {
        try
        {
            // Create WAV header
            byte[] wavData = AddWavHeader(rawData);
            
            // Create empty AudioClip
            AudioClip clip = CreateAudioClipFromWav(wavData);

            if (clip == null)
            {
                Debug.LogError("[AudioUtility] Failed to create AudioClip");
                return null;
            }

            return clip;
        }
        catch (Exception e)
        {
            Debug.LogError($"[AudioUtility] Error converting audio data: {e.Message}");
            return null;
        }
    }

    private static byte[] AddWavHeader(byte[] rawData)
    {
        const int headerSize = 44;
        byte[] wavFile = new byte[rawData.Length + headerSize];
        
        int sampleRate = 44100;
        ushort channels = 1;
        ushort bitDepth = 16;

        int fileSize = rawData.Length + headerSize - 8;
        int audioSize = rawData.Length;

        // RIFF header
        System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(wavFile, 0);
        BitConverter.GetBytes(fileSize).CopyTo(wavFile, 4);
        System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(wavFile, 8);

        // Format chunk
        System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(wavFile, 12);
        BitConverter.GetBytes(16).CopyTo(wavFile, 16); // Format chunk size
        BitConverter.GetBytes((ushort)1).CopyTo(wavFile, 20); // Audio format (1 = PCM)
        BitConverter.GetBytes(channels).CopyTo(wavFile, 22);
        BitConverter.GetBytes(sampleRate).CopyTo(wavFile, 24);
        BitConverter.GetBytes(sampleRate * channels * bitDepth / 8).CopyTo(wavFile, 28); // Byte rate
        BitConverter.GetBytes((ushort)(channels * bitDepth / 8)).CopyTo(wavFile, 32); // Block align
        BitConverter.GetBytes(bitDepth).CopyTo(wavFile, 34);

        // Data chunk
        System.Text.Encoding.ASCII.GetBytes("data").CopyTo(wavFile, 36);
        BitConverter.GetBytes(audioSize).CopyTo(wavFile, 40);
        rawData.CopyTo(wavFile, headerSize);

        return wavFile;
    }

    private static AudioClip CreateAudioClipFromWav(byte[] wavData)
    {
        // Read WAV header
        int channels = BitConverter.ToInt16(wavData, 22);
        int sampleRate = BitConverter.ToInt32(wavData, 24);
        int bitDepth = BitConverter.ToInt16(wavData, 34);
        int dataSize = BitConverter.ToInt32(wavData, 40);
        int samples = dataSize / (bitDepth / 8);

        // Convert samples to float array
        float[] audioData = new float[samples];
        int headerOffset = 44; // WAV header size

        for (int i = 0; i < samples; i++)
        {
            if (bitDepth == 16)
            {
                short sample = BitConverter.ToInt16(wavData, headerOffset + i * 2);
                audioData[i] = sample / 32768f;
            }
            else if (bitDepth == 8)
            {
                audioData[i] = (wavData[headerOffset + i] - 128) / 128f;
            }
        }

        AudioClip clip = AudioClip.Create(
            "StreamedVoice",
            samples,
            channels,
            sampleRate,
            false
        );

        clip.SetData(audioData, 0);
        return clip;
    }
}
