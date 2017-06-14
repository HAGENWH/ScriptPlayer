using System;
using System.Collections.Generic;
using System.Linq;
using ScriptPlayer.Shared;

namespace ScriptPlayer.VideoSync
{
    public class SampleAnalyser
    {
        private readonly SampleCondition _condition;
        private readonly AnalysisParameters _parameters;

        private readonly Dictionary<long, bool> _conditionFullfilled = new Dictionary<long, bool>();

        public SampleAnalyser(SampleCondition condition, AnalysisParameters parameters)
        {
            _condition = condition;
            _parameters = parameters;
        }

        public void AddSample(FrameCapture capture)
        {
            bool samplePositive = _condition.CheckSample(capture.Capture);
            long frameIndex = capture.FrameIndex;

            _conditionFullfilled.Add(frameIndex, samplePositive);
        }

        public List<long> GetResults()
        {
            List<Tuple<long, long>> beats = GetPositiveFrameRanges();
            List<long> result = new List<long>();

            foreach (Tuple<long, long> beat in beats)
            {
                switch (_parameters.Method)
                {
                    case TimeStampDeterminationMethod.FirstOccurence:
                        result.Add(beat.Item1);
                        break;
                    case TimeStampDeterminationMethod.Center:
                        result.Add((beat.Item2 + beat.Item1) / 2);
                        break;
                    case TimeStampDeterminationMethod.LastOccurence:
                        result.Add(beat.Item2);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return result;
        }


        private List<Tuple<long, long>> GetPositiveFrameRanges()
        {
            List<Tuple<long, bool>> samples = _conditionFullfilled.OrderBy(i => i.Key)
                .Select((kvp) => new Tuple<long, bool>(kvp.Key, kvp.Value)).ToList();

            List<Tuple<int, int>> resultIndices = new List<Tuple<int, int>>();

            bool beatActive = samples[0].Item2;
            int beatActiveSinceIndex = 0;

            int positivesInARow = 0;
            int negativesInARow = 0;

            for(int i = 0; i < samples.Count; i++)
            {
                if (samples[i].Item2)
                {
                    negativesInARow = 0;
                    if(positivesInARow == 0)
                        beatActiveSinceIndex = i;

                    positivesInARow++;
                    if (positivesInARow >= _parameters.MinPositiveSamples)
                    {
                        if (!beatActive)
                        {
                            beatActive = true;
                            beatActiveSinceIndex = i;
                        }
                    }
                }
                else
                {
                    positivesInARow = 0;
                    negativesInARow++;
                    if (negativesInARow >= _parameters.MinNegativeSamples)
                    {
                        if (beatActive)
                        {
                            beatActive = false;
                            resultIndices.Add(new Tuple<int, int>(beatActiveSinceIndex, i - 1));
                        }
                    }
                }
            }

            List<Tuple<long, long>> resultFrames = new List<Tuple<long, long>>();

            foreach (Tuple<int, int> indexRanges in resultIndices)
            {
                resultFrames.Add(new Tuple<long, long>(samples[indexRanges.Item1].Item1, samples[indexRanges.Item2].Item1));
            }

            return resultFrames;
        }
    }
}