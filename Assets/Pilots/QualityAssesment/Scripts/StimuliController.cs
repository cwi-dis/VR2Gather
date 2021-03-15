//using System.Collections;
using System.Collections.Generic;
using System.IO;
//using UnityEngine;
using VRT.Core;
//using VRT.Orchestrator.Wrapping;
//using System;
//using System.Linq;

namespace QualityAssesment
{
    public static class StimuliController
    {
        private static bool initialized = false;
        private static string currentStimulus = null;
        private static int stimuliIndex=-1;
        private static string[] stimuliList;
        private static List<QAStimuli> stimuliDescription = null;
        private static int codec;
        private static double bitrateBudget;

        private struct QAStimuli
        {
            public string stimuliName;
            public int codec;
            public double budget;
        }
        private static string Name()
        {
            return "StimuliController";
        }
        private static void init()
        {
            //Load stimuli description csv used to set bit rate budgets for each seuence + codec + ratepoint
            string rootFolder = System.IO.Directory.GetParent(Config.Instance.LocalUser.PCSelfConfig.PrerecordedReaderConfig.folder).ToString();
            string csvFilename = System.IO.Path.Combine(rootFolder,"stimulidescription.csv");
            stimuliDescription = new List<QAStimuli>();
            FileInfo stimuliDescFile = new FileInfo(csvFilename);
            if (!stimuliDescFile.Exists)
            {
                stimuliDescription = null; // Delete stimuli description datastructure to forestall further errors
                throw new System.Exception($"Stimuli description not found at {csvFilename}");
            }
            StreamReader stimuliDescReader = stimuliDescFile.OpenText();
            //Skip header
            var sLine = stimuliDescReader.ReadLine();
            QAStimuli sFrame = new QAStimuli();
            while ((sLine = stimuliDescReader.ReadLine()) != null)
            {
                var sLineValues = sLine.Split(',');
                sFrame.stimuliName = sLineValues[0];
                sFrame.codec = int.Parse(sLineValues[1]);
                sFrame.budget = double.Parse(sLineValues[2]);
                stimuliDescription.Add(sFrame);
                sFrame = new QAStimuli();
            }
            //Load stimuli list from config.json
            stimuliList = Config.Instance.stimuliList;
            stimuliIndex = 0;
            initialized = true;
        }
        public static bool loadnext()
        {
            if (!initialized)
                init();
            currentStimulus = stimuliList[stimuliIndex];
            QAStimuli sframe = stimuliDescription.Find(x => x.stimuliName == currentStimulus);
            bitrateBudget = sframe.budget;
            codec = sframe.codec;
            //Set the instanceconig variable so the readers are initialized correctly
            Config._User realUser = Config.Instance.LocalUser;
            realUser.PCSelfConfig.PrerecordedReaderConfig.folder = System.IO.Path.Combine(Config.Instance.rootFolder, "H" + currentStimulus[1]);
            //xxxshishir set tilefolders to null if stimulus is not tiled
            stimuliIndex++;
            if (stimuliIndex == stimuliList.Length)
            {
                stimuliIndex = 0;
                return false;
            }
            else
                return true;
        }
        public static string getCurrentStimulus()
        {
            if (stimuliIndex == -1)
                return "InitSequence";
            else
                return currentStimulus;
        }
        public static double getBitrateBudget()
        {
            return bitrateBudget;
        }
        public static int getCodec()
        {
            return codec;
        }
        public static bool isTiled()
        {
            if (codec >= 3 && codec <= 5)
                return true;
            else
                return false;
        }
    }
}
