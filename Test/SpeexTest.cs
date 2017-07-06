using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using NUnit.Framework;

namespace NSpeex.Test
{
    public class SpeexTest
    {

        private static FileInfo GetWavFile(string name)
        {
            DirectoryInfo audioDir = GetAudioDir();
            FileInfo file = audioDir.GetFiles(name)[0];
            return file;
        }

        private static DirectoryInfo GetAudioDir()
        {
            string path = Directory.GetCurrentDirectory();
            DirectoryInfo dir = new DirectoryInfo(path).Parent.Parent;
            DirectoryInfo audioDir = dir.GetDirectories("Audio")[0];
            return audioDir;
        }


        [Test]
        public void FileInfoTest()
        {
            const string name = "male.wav";
            const int HEAD_SIZE = 8;
            const string CHUNK_DATA = "data";

            FileInfo file = GetWavFile(name);
            Console.WriteLine("file name:" + file.FullName);
            FileStream fs = File.OpenRead(file.FullName);
            byte[] tmp = new byte[2560];
            fs.Read(tmp, 0, HEAD_SIZE + 4);

            String ckID = LittleEndian.ReadString(tmp, 0, 4);
            Console.WriteLine("ckID,0-3:" + ckID);
            int cksize = LittleEndian.ReadInt(tmp, 4);
            Console.WriteLine("cksize,4-7:" + cksize);
            String WAVEID = LittleEndian.ReadString(tmp, 8, 4);
            Console.WriteLine("WAVEID,8-11:" + WAVEID);

            fs.Read(tmp, 0, HEAD_SIZE);
            String chunk = LittleEndian.ReadString(tmp, 0, 4);
            int size = LittleEndian.ReadInt(tmp, 4);

            //
            short format = 0;
            short nChannels = 0;
            int nSamplesPerSec = 0;
            int nAvgBytesPerSec = 0;
            short nBlockAlign = 0;
            short wBitsPerSample = 0;
            while (!chunk.Equals(CHUNK_DATA))
            {
                Console.WriteLine("chunk:" + chunk);
                Console.WriteLine("size:" + size);
                // read size bytes
                fs.Read(tmp, 0, size);
                format = LittleEndian.ReadShort(tmp, 0);
                Console.WriteLine("format,0-1:" + format);

                nChannels = LittleEndian.ReadShort(tmp, 2);
                Console.WriteLine("nChannels,2-3:" + nChannels);

                nSamplesPerSec = LittleEndian.ReadInt(tmp, 4);
                Console.WriteLine("nSamplesPerSec,4-7:" + nSamplesPerSec);

                nAvgBytesPerSec = LittleEndian.ReadInt(tmp, 8);
                Console.WriteLine("nAvgBytesPerSec:" + nAvgBytesPerSec);

                nBlockAlign = LittleEndian.ReadShort(tmp, 12);
                Console.WriteLine("nBlockAlign:" + nBlockAlign);

                wBitsPerSample = LittleEndian.ReadShort(tmp, 14);
                Console.WriteLine("wBitsPerSample:" + wBitsPerSample);

                fs.Read(tmp, 0, HEAD_SIZE);
                chunk = LittleEndian.ReadString(tmp, 0, 4);
                size = LittleEndian.ReadInt(tmp, 4);
                Console.WriteLine("chunk:" + chunk);
                Console.WriteLine("size:" + size);
            }

            BandMode mode;
            if (nSamplesPerSec < 12000)
            {
                // Narrowband
                mode = BandMode.Narrow;
            }
            else if (nSamplesPerSec < 24000)
            {
                // Wideband
                mode = BandMode.Wide;
            }
            else
            {
                //Ultra-wideband
                mode = BandMode.UltraWide;
            }

            SpeexEncoder speexEncoder = new SpeexEncoder(mode);
            int quality = 2;// modify your self
            int framesPerPackage = 4;// one frame one package
            bool vbr = false;
           


            //            speexEncoder.getEncoder().setComplexity(5);
            //            speexEncoder.getEncoder().setBitRate();
            //            speexEncoder.getEncoder().setVbr();
            //            speexEncoder.getEncoder().setVbrQuality();
            //            speexEncoder.getEncoder().setVad();
            //            speexEncoder.getEncoder().setDtx();

            OggSpeexWriter2 writer = new OggSpeexWriter2(mode, nSamplesPerSec, nChannels, framesPerPackage, vbr, nAvgBytesPerSec);
            writer.Open("result.spx");
            writer.WriteHeader("alking");
            int pcmPacketSize = 2 * nChannels * speexEncoder.FrameSize;
            int bytesCount = 0;

         
            
            while (bytesCount < size)
            {
                int read = pcmPacketSize * framesPerPackage;
                fs.Read(tmp, 0, read);
                short[] data = new short[read / 2];
                Buffer.BlockCopy(tmp, 0, data, 0, read);
                
                int encSize = speexEncoder.Encode(data, 0, data.Length, tmp, 0, tmp.Length);
                if (encSize > 0)
                {
                   writer.WritePackage(tmp,0,encSize);

                }
                bytesCount += read;

            }
            writer.Close();
            fs.Close();

        }
    }
}
