using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;
using System.Text;
using VRTCore;
using System.Net.Http;
using VRT.Orchestrator.Wrapping;

namespace VRT.UserRepresentation.TVM
{
    public class PerformanceMetrics : MonoBehaviour
    {
        private static System.Diagnostics.Process performanceProcess = new System.Diagnostics.Process();
        private static System.Diagnostics.Process RMQprocess = new System.Diagnostics.Process();
        private string fullPathToExe = "";
        private bool tenSecondsReached = false;
        private List<double> deserializeTime = new List<double>();
        private List<double> renderingTime = new List<double>();
        private List<double> deserializeFunctionTime = new List<double>();
        private List<double> deserializeTexturesTime = new List<double>();
        private List<double> deserializeGeometryTime = new List<double>();
        private List<double> deserializeParamsTime = new List<double>();
        private List<double> frameTime = new List<double>();
        private List<string> metric_units = new List<string>() { "KBs", "MBs", "GBs" };
        private long totalTime = 0;
        private int fps = 0;
        private int all_frames = 0;
        private List<int> compressed_buffer_size = new List<int>();
        private List<int> decompressed_buffer_size = new List<int>();
        private List<int> num_vertices = new List<int>();
        private List<int> endToEndDelay = new List<int>();
        private StringBuilder metricsCSV = new StringBuilder();
        private List<double> usageSamplesCPU = new List<double>();
        private List<double> usageSamplesGPU = new List<double>();
        private List<double> usageSamplesRAM = new List<double>();
        private List<double> usageSamplesBW = new List<double>();
        private SortedDictionary<string, Dictionary<string, string>> nodesMemoryData = new SortedDictionary<string, Dictionary<string, string>>();
        private SortedDictionary<string, Dictionary<string, string>> exchangesData = new SortedDictionary<string, Dictionary<string, string>>();
        private SortedDictionary<string, Dictionary<string, string>> connectionsData = new SortedDictionary<string, Dictionary<string, string>>();
        private List<string> connectionNames = new List<string>();
        private List<string> exchangeNames = new List<string>();
        private static readonly HttpClient client = new HttpClient();
        private int userNum = 0;
        private List<string> connectedUserIds = new List<string>();

        private void Awake()
        {
            fullPathToExe = Application.streamingAssetsPath + "\\PerformanceCounting";
            connectedUserIds = OrchestratorController.Instance.ConnectedUsers.Select(x => x.userId).ToList();
            exchangeNames.Add(Config.Instance.TVMs.exchangeName);
            OrchestratorController.Instance.OnGetUserInfoEvent += OnGetUserInfoHandler;
            OrchestratorController.Instance.GetUserInfo(connectedUserIds[userNum]);
        }

        private void OnGetUserInfoHandler(User user)
        {
            if (!string.IsNullOrEmpty(user.userData.userIP) && !connectionNames.Contains(user.userData.userIP))
                connectionNames.Add(user.userData.userIP);

            connectionNames.Add(user.userData.userIP);
            userNum++;

            if (userNum >= connectedUserIds.Count)
            {
                connectionNames.Add(OrchestratorController.Instance.GetIPAddress());
                OrchestratorController.Instance.OnGetUserInfoEvent -= OnGetUserInfoHandler;
                runRMQMetricsExe();
                return;
            }
            else
                OrchestratorController.Instance.GetUserInfo(connectedUserIds[userNum]);
        }

        public void updateReceivingAndDeserializationMetrics(List<System.Diagnostics.Stopwatch> deserializationStopWatches, List<int> tvmData)
        {

            if (!Config.Instance.TVMs.printMetrics && !Config.Instance.TVMs.saveMetrics)
                return;

            compressed_buffer_size.Add(tvmData[0]);
            decompressed_buffer_size.Add(tvmData[1]);
            num_vertices.Add(tvmData[2]);
            all_frames = tvmData[3];
            deserializeTime.Add(deserializationStopWatches[0].ElapsedMilliseconds);
            deserializeFunctionTime.Add(deserializationStopWatches[1].Elapsed.TotalMilliseconds);
            deserializeTexturesTime.Add(deserializationStopWatches[2].Elapsed.TotalMilliseconds);
            deserializeGeometryTime.Add(deserializationStopWatches[3].Elapsed.TotalMilliseconds);
            deserializeParamsTime.Add(deserializationStopWatches[4].Elapsed.TotalMilliseconds);
        }

        public void updateRenderingMetrics(System.Diagnostics.Stopwatch renderingStopWatch, System.Diagnostics.Stopwatch frametime, int E2Edelay)
        {
            if (!Config.Instance.TVMs.printMetrics && !Config.Instance.TVMs.saveMetrics)
                return;

            ++fps;
            endToEndDelay.Add(E2Edelay);
            renderingTime.Add(renderingStopWatch.ElapsedMilliseconds);
            frameTime.Add(frametime.ElapsedMilliseconds);
        }

        private double CalculateStdDev(IEnumerable<double> values)
        {
            double ret = 0;
            if (values.Count() > 0)
            {
                //Compute the Average      
                double avg = values.Average();
                //Perform the Sum of (value-avg)_2_2      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));
                //Put it all together      
                ret = Math.Sqrt(sum / (values.Count() - 1));
            }
            return ret;
        }

        private double changeMetricUnit(double metric, int metric_unit)
        {
            for (int i = 0; i < metric_unit + 1; i++)
                metric /= 1024;

            return metric;
        }

        public void printMetrics(System.Diagnostics.Stopwatch stopWatch, string tvmObjName)
        {
            if (!Config.Instance.TVMs.printMetrics)
                return;
            try
            {
                tenSecondsReached = true;
                totalTime += stopWatch.ElapsedMilliseconds;

                if (Math.Abs(stopWatch.ElapsedMilliseconds - 10000) > 500)
                    return;

                Debug.Log("sw (ms) = " + totalTime + ": General Statistics (TVM Object Name: " + tvmObjName +
                "): \n                                      Frames per second (number of frames / time): " + ((double)fps / stopWatch.ElapsedMilliseconds * 1000).ToString("F8") +
                "\n                                      Average Frametime: " + (!frameTime.Any() ? "None" : frameTime.Average().ToString("F8")) + " ms" +
                                                        " (Standard deviation: " + (!frameTime.Any() ? "None" : CalculateStdDev(frameTime).ToString("F8")) + ")" +
                "\n                                      Frames per second (1 / Average Frametime in seconds): " + (!frameTime.Any() ? "None" : (1000 / frameTime.Average()).ToString("F8")) +
                "\n                                      Number of TVMs received but not rendered: " + (all_frames - fps <= 0 ? 0 : all_frames - fps) +
                "\n                                      Average end to end delay: " + (!endToEndDelay.Any() ? "None" : endToEndDelay.Average().ToString("F8")) + " ms" +
                                                        " (Standard deviation: " + (!endToEndDelay.Any() ? "None" : CalculateStdDev(endToEndDelay.Select(x => Convert.ToDouble(x))).ToString("F8")) + ")" +
                "\n                                      Average number of vertices per TVM: " + (!num_vertices.Any() ? "None" : num_vertices.Average().ToString("F8")) +
                                                        " (Standard deviation: " + (!num_vertices.Any() ? "None" : CalculateStdDev(num_vertices.Select(x => Convert.ToDouble(x))).ToString("F8")) + ")");

                Debug.Log("sw (ms) = " + totalTime + ": Compression Module (TVM Object Name: " + tvmObjName +
                "): \n                                      Average compressed TVM buffer size (" + metric_units[1] + "): " + (!compressed_buffer_size.Any() ? "None" : changeMetricUnit(Convert.ToDouble(compressed_buffer_size.Average()), 1).ToString("F8")) + " " +
                                                        " (Standard deviation: " + (!compressed_buffer_size.Any() ? "None" : changeMetricUnit(CalculateStdDev(compressed_buffer_size.Select(x => Convert.ToDouble(x))), 1).ToString("F8")) + ")" +
                "\n                                      Average decompressed TVM buffer size (" + metric_units[1] + "): " + (!decompressed_buffer_size.Any() ? "None" : changeMetricUnit(Convert.ToDouble(decompressed_buffer_size.Average()), 1).ToString("F8")) + " " +
                                                        " (Standard deviation: " + (!decompressed_buffer_size.Any() ? "None" : changeMetricUnit(CalculateStdDev(decompressed_buffer_size.Select(x => Convert.ToDouble(x))), 1).ToString("F8")) + ")");

                Debug.Log("sw (ms) = " + totalTime + ":  Deserialization-Decompression Module (TVM Object Name: " + tvmObjName +
                "): \n                                      Average total deserialization-decompression time per TVM: " + (!deserializeTime.Any() ? "None" : deserializeTime.Average().ToString("F8")) + " ms" +
                                                        " (Standard deviation: " + (!deserializeTime.Any() ? "None" : CalculateStdDev(deserializeTime).ToString("F8")) + ")" +
                "\n                                      Average time of deserialization-decompression function call per TVM: " + (!deserializeFunctionTime.Any() ? "None" : deserializeFunctionTime.Average().ToString("F8")) + " ms" +
                                                        " (Standard deviation: " + (!deserializeFunctionTime.Any() ? "None" : CalculateStdDev(deserializeFunctionTime).ToString("F8")) + ")" +
                "\n                                      Average deserialization time of texture data per TVM: " + (!deserializeTexturesTime.Any() ? "None" : deserializeTexturesTime.Average().ToString("F8")) + " ms" +
                                                        " (Standard deviation: " + (!deserializeTexturesTime.Any() ? "None" : CalculateStdDev(deserializeTexturesTime).ToString("F8")) + ")" +
                "\n                                      Average deserialization time of geometry (faces, vertices) data per TVM: " + (!deserializeGeometryTime.Any() ? "None" : deserializeGeometryTime.Average().ToString("F8")) + " ms" +
                                                        " (Standard deviation: " + (!deserializeGeometryTime.Any() ? "None" : CalculateStdDev(deserializeGeometryTime).ToString("F8")) + ")" +
                "\n                                      Average deserialization time of parameters per TVM: " + (!deserializeParamsTime.Any() ? "None" : deserializeParamsTime.Average().ToString("F8")) + " ms" +
                                                        " (Standard deviation: " + (!deserializeParamsTime.Any() ? "None" : CalculateStdDev(deserializeParamsTime).ToString("F8")) + ")");

                Debug.Log("sw (ms) = " + totalTime + ": Rendering Module (TVM Object Name: " + tvmObjName +
                "): \n                                      Average rendering time per TVM: " + (!renderingTime.Any() ? "None" : renderingTime.Average().ToString("F8")) + " ms" +
                                                        " (Standard deviation: " + (!renderingTime.Any() ? "None" : CalculateStdDev(renderingTime).ToString("F8")) + ")");

                Debug.Log("sw (ms) = " + totalTime + ": PC Consumptions (TVM Object Name: " + tvmObjName +
                "): \n                                      Average CPU% usage: " + (!usageSamplesCPU.Any() ? "None" : usageSamplesCPU.Average().ToString("F8")) +
                                                        " (Standard deviation: " + (!usageSamplesCPU.Any() ? "None" : CalculateStdDev(usageSamplesCPU).ToString("F8")) + ")" +
                "\n                                      Average GPU% usage: " + (!usageSamplesGPU.Any() ? "None" : usageSamplesGPU.Average().ToString("F8")) +
                                                        " (Standard deviation: " + (!usageSamplesGPU.Any() ? "None" : CalculateStdDev(usageSamplesGPU).ToString("F8")) + ")" +
                "\n                                      Average RAM usage (MBs): " + (!usageSamplesRAM.Any() ? "None" : usageSamplesRAM.Average().ToString("F8")) +
                                                        " (Standard deviation: " + (!usageSamplesRAM.Any() ? "None" : CalculateStdDev(usageSamplesRAM).ToString("F8")) + ")" +
                "\n                                      Average BW usage (MBps): " + (!usageSamplesBW.Any() ? "None" : usageSamplesBW.Average().ToString("F8")) +
                                                        " (Standard deviation: " + (!usageSamplesBW.Any() ? "None" : CalculateStdDev(usageSamplesBW).ToString("F8")) + ")");

                Debug.Log("sw (ms) = " + totalTime + ": RMQ Consumptions (TVM Object Name: " + tvmObjName +
                "): \n                                      RMQ Exchanges:" + formatRMQMetricsOutputString(exchangesData, "RMQ exchange name", -1) +
                "\n                                      Peer connections to RMQ server " + Config.Instance.TVMs.connectionURI.Split('@')[1].Split(':')[0] + ":" + formatRMQMetricsOutputString(connectionsData, "Peer 's IP(:Port)", 1) +
                "\n                                      RMQ server 's " + Config.Instance.TVMs.connectionURI.Split('@')[1].Split(':')[0] + " memory (MBs):" + formatRMQMetricsOutputString(nodesMemoryData, "RMQ node 's name", 1));
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return;
            }
        }

        private string formatRMQMetricsOutputString(SortedDictionary<string, Dictionary<string, string>> data, string typeData, int metricsUnit)
        {
            string outputString = "";
            List<string> valuesOfInterest = new List<string>();

            foreach (var key in data.Keys)
            {
                outputString += "\n                                                 " + typeData + ": " + key;

                for (int i = 0; i < data[key].Count; i++)
                {
                    if (data[key].ElementAt(i).Value.Split(',').Length > 0)
                    {
                        valuesOfInterest = data[key].ElementAt(i).Value.Split(',').ToList();
                        valuesOfInterest.RemoveAll(x => x.Contains("None"));

                        if (!data[key].ElementAt(i).Key.Contains("strategy"))
                        {
                            if (metricsUnit < 0)
                                outputString += "\n                                                             " + data[key].ElementAt(i).Key + ": " +
                                                    (!valuesOfInterest.Any() ? "None" : valuesOfInterest.Select(x => double.Parse(x)).ToList().Average().ToString("F8")) +
                                            " (Standard deviation: " + (!valuesOfInterest.Any() ? "None" : CalculateStdDev(valuesOfInterest.Select(x => double.Parse(x)).ToList()).ToString("F8")) + ")";
                            else
                                outputString += "\n                                                             " + data[key].ElementAt(i).Key + ": " +
                                                    (!valuesOfInterest.Any() ? "None" : changeMetricUnit(valuesOfInterest.Select(x => double.Parse(x)).ToList().Average(), metricsUnit).ToString("F8")) +
                                            " (Standard deviation: " + (!valuesOfInterest.Any() ? "None" : changeMetricUnit(CalculateStdDev(valuesOfInterest.Select(x => double.Parse(x)).ToList()), metricsUnit).ToString("F8")) + ")";
                        }
                        else
                        {
                            outputString += "\n                                                             " + data[key].ElementAt(i).Key + ": " +
                                                    (!valuesOfInterest.Any() ? "None" : string.Join("x", new HashSet<string>(valuesOfInterest)));
                        }
                    }
                }
            }
            return string.IsNullOrEmpty(outputString) ? "\n                                                             None" : outputString;
        }

        private string formatRMQMetricsSavedString(SortedDictionary<string, Dictionary<string, string>> data, int metricsUnit)
        {
            string outputString = "";
            List<string> valuesOfInterest = new List<string>();

            foreach (var key in data.Keys)
            {
                for (int i = 0; i < data[key].Count; i++)
                {
                    if (data[key].ElementAt(i).Value.Split(',').Length > 0)
                    {
                        valuesOfInterest = data[key].ElementAt(i).Value.Split(',').ToList();
                        valuesOfInterest.RemoveAll(x => x.Contains("None"));

                        if (!data[key].ElementAt(i).Key.Contains("strategy"))
                        {
                            if (metricsUnit < 0)
                                outputString += "," + (!valuesOfInterest.Any() ? "None" : valuesOfInterest.Select(x => double.Parse(x)).ToList().Average().ToString("F8")) +
                                            "," + (!valuesOfInterest.Any() ? "None" : CalculateStdDev(valuesOfInterest.Select(x => double.Parse(x)).ToList()).ToString("F8"));
                            else
                                outputString += "," + (!valuesOfInterest.Any() ? "None" : changeMetricUnit(valuesOfInterest.Select(x => double.Parse(x)).ToList().Average(), metricsUnit).ToString("F8")) +
                                            "," + (!valuesOfInterest.Any() ? "None" : changeMetricUnit(CalculateStdDev(valuesOfInterest.Select(x => double.Parse(x)).ToList()), metricsUnit).ToString("F8"));
                        }
                        else
                        {
                            outputString += "," + (!valuesOfInterest.Any() ? "None" : string.Join("x", new HashSet<string>(valuesOfInterest)));
                        }

                        valuesOfInterest.Clear();
                    }
                }
            }

            return outputString;
        }

        private string formatRMQMetricsNamesSavedString(SortedDictionary<string, Dictionary<string, string>> data, string typeData)
        {
            string outputString = "";

            foreach (var key in data.Keys)
            {
                for (int i = 0; i < data[key].Count; i++)
                {
                    if (data[key].ElementAt(i).Value.Split(',').Length > 0)
                    {
                        if (!data[key].ElementAt(i).Key.Contains("strategy"))
                        {
                            outputString += ",Average of " + data[key].ElementAt(i).Key + typeData + key;
                            outputString += ",Standard deviation of " + data[key].ElementAt(i).Key + typeData + key;
                        }
                        else
                            outputString += ",Strategy of" + typeData + key;
                    }
                }
            }

            return outputString;
        }

        public void saveMetrics(System.Diagnostics.Stopwatch stopWatch)
        {
            if (!Config.Instance.TVMs.saveMetrics)
                return;

            try
            {
                if (Math.Abs(stopWatch.ElapsedMilliseconds - 10000) > 500)
                    return;

                string metricsLastTen = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33}",
                                                        DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss"), ((double)fps / stopWatch.ElapsedMilliseconds * 1000).ToString("F8"), (all_frames - fps <= 0 ? 0 : all_frames - fps).ToString(),
                                                        !frameTime.Any() ? "None" : frameTime.Average().ToString("F8"), !frameTime.Any() ? "None" : CalculateStdDev(frameTime).ToString("F8"),
                                                        !frameTime.Any() ? "None" : (1000 / frameTime.Average()).ToString("F8"),
                                                        !endToEndDelay.Any() ? "None" : endToEndDelay.Average().ToString("F8"), !endToEndDelay.Any() ? "None" : CalculateStdDev(endToEndDelay.Select(x => Convert.ToDouble(x))).ToString("F8"),
                                                        !num_vertices.Any() ? "None" : num_vertices.Average().ToString("F8"), !num_vertices.Any() ? "None" : CalculateStdDev(num_vertices.Select(x => Convert.ToDouble(x))).ToString("F8"),
                                                        !compressed_buffer_size.Any() ? "None" : changeMetricUnit(Convert.ToDouble(compressed_buffer_size.Average()), 1).ToString("F8"),
                                                        !compressed_buffer_size.Any() ? "None" : changeMetricUnit(CalculateStdDev(compressed_buffer_size.Select(x => Convert.ToDouble(x))), 1).ToString("F8"),
                                                        !decompressed_buffer_size.Any() ? "None" : changeMetricUnit(Convert.ToDouble(decompressed_buffer_size.Average()), 1).ToString("F8"),
                                                        !decompressed_buffer_size.Any() ? "None" : changeMetricUnit(CalculateStdDev(decompressed_buffer_size.Select(x => Convert.ToDouble(x))), 1).ToString("F8"),
                                                        !deserializeTime.Any() ? "None" : deserializeTime.Average().ToString("F8"), !deserializeTime.Any() ? "None" : CalculateStdDev(deserializeTime).ToString("F8"),
                                                        !deserializeFunctionTime.Any() ? "None" : deserializeFunctionTime.Average().ToString("F8"), !deserializeFunctionTime.Any() ? "None" : CalculateStdDev(deserializeFunctionTime).ToString("F8"),
                                                        !deserializeTexturesTime.Any() ? "None" : deserializeTexturesTime.Average().ToString("F8"), !deserializeTexturesTime.Any() ? "None" : CalculateStdDev(deserializeTexturesTime).ToString("F8"),
                                                        !deserializeGeometryTime.Any() ? "None" : deserializeGeometryTime.Average().ToString("F8"), !deserializeGeometryTime.Any() ? "None" : CalculateStdDev(deserializeGeometryTime).ToString("F8"),
                                                        !deserializeParamsTime.Any() ? "None" : deserializeParamsTime.Average().ToString("F8"), !deserializeParamsTime.Any() ? "None" : CalculateStdDev(deserializeParamsTime).ToString("F8"),
                                                        !renderingTime.Any() ? "None" : renderingTime.Average().ToString("F8"), !renderingTime.Any() ? "None" : CalculateStdDev(renderingTime).ToString("F8"),
                                                        !usageSamplesCPU.Any() ? "None" : usageSamplesCPU.Average().ToString("F8"), !usageSamplesCPU.Any() ? "None" : CalculateStdDev(usageSamplesCPU).ToString("F8"),
                                                        !usageSamplesGPU.Any() ? "None" : usageSamplesGPU.Average().ToString("F8"), !usageSamplesGPU.Any() ? "None" : CalculateStdDev(usageSamplesGPU).ToString("F8"),
                                                        !usageSamplesRAM.Any() ? "None" : usageSamplesRAM.Average().ToString("F8"), !usageSamplesRAM.Any() ? "None" : CalculateStdDev(usageSamplesRAM).ToString("F8"),
                                                        !usageSamplesBW.Any() ? "None" : usageSamplesBW.Average().ToString("F8"), !usageSamplesBW.Any() ? "None" : CalculateStdDev(usageSamplesBW).ToString("F8")) +
                                                        formatRMQMetricsSavedString(exchangesData, -1) +
                                                        formatRMQMetricsSavedString(connectionsData, 1) +
                                                        formatRMQMetricsSavedString(nodesMemoryData, 1);

                if (metricsCSV.Length == 0)
                {
                    string metricsNames = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24},{25},{26},{27},{28},{29},{30},{31},{32},{33}",
                                                        "Timestamp", "Frames per second (number of frames / time)", "Missed / Skipped frames",
                                                        "Average frametime per TVM (milliseconds) ", "Standard deviation of average frametime per TVM (milliseconds)",
                                                        "Frames per second (1 / Average frametime in seconds)",
                                                        "Average end to end delay (milliseconds)", "Standard deviation of end to end delay (milliseconds)",
                                                        "Average number of vertices per TVM", "Standard deviation of number of vertices per TVM",
                                                        "Average size of received compressed TVM (" + metric_units[1] + ")", "Standard deviation of received compressed TVM 's size (" + metric_units[1] + ")",
                                                        "Average size of decompressed TVM (" + metric_units[1] + ")", "Standard deviation of decompressed TVM 's size (" + metric_units[1] + ")",
                                                        "Average total deserialization-decompression time per TVM (milliseconds)", "Standard deviation of deserialization-decompression time per TVM (milliseconds)",
                                                        "Average deserialization-decompression function execution time per TVM (milliseconds)", "Standard deviation of deserialization-decompression function execution time  per TVM (milliseconds)",
                                                        "Average marshalling time for the texture data per TVM (milliseconds)", "Standard deviation of marshalling time for the texture data per TVM (milliseconds)",
                                                        "Average marshalling time for the geometry data per TVM (milliseconds)", "Standard deviation of marshalling time for the geometry data per TVM (milliseconds)",
                                                        "Average marshalling time for the extra parameters per TVM (milliseconds)", "Standard deviation of marshalling time for the extra parameters per TVM (milliseconds)",
                                                        "Average rendering time per TVM (milliseconds)", "Standard deviation of rendering time per TVM (milliseconds)",
                                                        "Average CPU % consumption", "Standard deviation of CPU % consumption",
                                                        "Average GPU % consumption", "Standard deviation of GPU % consumption",
                                                        "Average RAM consumption (MBs)", "Standard deviation of RAM consumption (MBs)",
                                                        "Average BW consumption (Mbps)", "Standard deviation of BW consumption (Mbps)") +
                                                        formatRMQMetricsNamesSavedString(exchangesData, " of RMQ exchange ") +
                                                        formatRMQMetricsNamesSavedString(connectionsData, " of RMQ connection ") +
                                                        formatRMQMetricsNamesSavedString(nodesMemoryData, " memory (MBs) of RMQ server 's node ");

                    metricsCSV.AppendLine(metricsNames);
                }

                metricsCSV.AppendLine(metricsLastTen);
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return;
            }
        }

        public void saveAndDestroy(string tvmObjName)
        {
            if (!Config.Instance.TVMs.printMetrics && !Config.Instance.TVMs.saveMetrics)
                return;

            if (!performanceProcess.HasExited)
                performanceProcess.Kill();
            if (!RMQprocess.HasExited)
                RMQprocess.Kill();

            if (Config.Instance.TVMs.saveMetrics)
            {
                string[] allLines = metricsCSV.ToString().Split('\n');

                string expDirectory = Path.Combine(Directory.GetParent(Environment.GetEnvironmentVariable("AppData")).ToString(), "LocalLow", Application.companyName, Application.productName, "TVMPerfomanceLogs", DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss"));
                if (!Directory.Exists(expDirectory))
                    Directory.CreateDirectory(expDirectory);

                File.WriteAllText(expDirectory + "\\PerformanceMetrics_" + tvmObjName + ".csv", metricsCSV.ToString());
            }

            metricsCSV.Clear();
        }

        public void clearAll()
        {
            if (!Config.Instance.TVMs.printMetrics && !Config.Instance.TVMs.saveMetrics)
                return;

            fps = 0;
            compressed_buffer_size.Clear();
            decompressed_buffer_size.Clear();
            num_vertices.Clear();
            frameTime.Clear();
            endToEndDelay.Clear();
            all_frames = 0;
            renderingTime.Clear();
            deserializeTime.Clear();
            deserializeParamsTime.Clear();
            deserializeGeometryTime.Clear();
            deserializeTexturesTime.Clear();
            deserializeFunctionTime.Clear();
            usageSamplesCPU.Clear();
            usageSamplesGPU.Clear();
            usageSamplesRAM.Clear();
            usageSamplesBW.Clear();
            nodesMemoryData.Clear();
            exchangesData.Clear();
            connectionsData.Clear();
            tenSecondsReached = false;
        }

        public void runCommand()
        {
            runPerfomanceMetricsExe();
        }

        public void runPerfomanceMetricsExe()
        {
            if (!Config.Instance.TVMs.printMetrics && !Config.Instance.TVMs.saveMetrics)
                return;

            performanceProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fullPathToExe + "\\PerfomanceData",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                }
            };

            //* Set your output and error (asynchronous) handlers
            performanceProcess.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(PerformanceOutputHandler);
            performanceProcess.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(PerformanceErrorHandler);
            performanceProcess.Start();
            performanceProcess.BeginOutputReadLine();
            performanceProcess.BeginErrorReadLine();
        }

        private void runRMQMetricsExe()
        {
            if (!Config.Instance.TVMs.printMetrics && !Config.Instance.TVMs.saveMetrics)
                return;

            RMQprocess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fullPathToExe + "\\RMQData",
                    Arguments = Config.Instance.TVMs.connectionURI.Split('@')[1].Split(':')[0] + " " + // RMQ Server 's ip
                                Config.Instance.TVMs.connectionURI.Split('@')[0].Split(new string[] { "//" }, StringSplitOptions.None)[1].Split(':')[0] + " " + // RMQ Server 's username
                                Config.Instance.TVMs.connectionURI.Split('@')[0].Split(new string[] { "//" }, StringSplitOptions.None)[1].Split(':')[1] + " " + // RMQ Server 's password
                                (exchangeNames.Where(s => !string.IsNullOrWhiteSpace(s)).ToList().Count > 0 ? string.Join(",", exchangeNames.Where(s => !string.IsNullOrWhiteSpace(s)).ToList()) : "None") + " " + // RMQ Server 's exchange name
                                (connectionNames.Where(s => !string.IsNullOrWhiteSpace(s)).ToList().Count > 0 ? string.Join(",", connectionNames.Where(s => !string.IsNullOrWhiteSpace(s)).ToList()) : "None") + " " + // RMQ Server 's connections
                                "1000", // period
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                }
            };

            //* Set your output and error (asynchronous) handlers
            RMQprocess.OutputDataReceived += new System.Diagnostics.DataReceivedEventHandler(RMQOutputHandler);
            RMQprocess.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(RMQErrorHandler);
            RMQprocess.Start();
            RMQprocess.BeginOutputReadLine();
            RMQprocess.BeginErrorReadLine();
        }

        private void RMQErrorHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            if (!tenSecondsReached && outLine.Data.Length > 0)
            {
                Debug.Log("RMQ Metrics Executable Error: " + outLine.Data);
                RMQprocess.Kill();
                runRMQMetricsExe();
            }
        }

        private void RMQOutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            if (!tenSecondsReached && outLine.Data.Length > 0)
            {
                string dataDescription = outLine.Data.Split(new string[] { " -> " }, StringSplitOptions.None)[0];

                if (dataDescription.Contains("Exchange"))
                    parseRMQMessage(exchangesData, outLine.Data);
                else if (dataDescription.Contains("memory"))
                    parseRMQMessage(nodesMemoryData, outLine.Data);
                else if (dataDescription.Contains("IP(:Port)"))
                    parseRMQMessage(connectionsData, outLine.Data);
            }
        }

        private void parseRMQMessage(SortedDictionary<string, Dictionary<string, string>> data, string outLine)
        {
            string dataDescription = outLine.Split(new string[] { " -> " }, StringSplitOptions.None)[0];
            string dataValue = outLine.Split(new string[] { " -> " }, StringSplitOptions.None)[1];
            dataValue = dataValue.Contains("(bytes / second)") ? dataValue.Replace("(bytes / second)", "(MBs / second)") : dataValue;

            if (!data.ContainsKey(dataDescription.Split(' ')[2]))
                data[dataDescription.Split(' ')[2]] = new Dictionary<string, string>();
            if (!data[dataDescription.Split(' ')[2]].ContainsKey(dataValue.Split(':')[0]))
                data[dataDescription.Split(' ')[2]][dataValue.Split(new string[] { ": " }, StringSplitOptions.None)[0]] = dataValue.Split(' ').Last();
            else
                data[dataDescription.Split(' ')[2]][dataValue.Split(new string[] { ": " }, StringSplitOptions.None)[0]] += "," + dataValue.Split(' ').Last();
        }

        private void PerformanceErrorHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            if (outLine.Data.Length > 0)
            {
                Debug.Log("Perfomance Metrics Executable Error: " + outLine.Data);
                performanceProcess.Kill();
                runPerfomanceMetricsExe();
            }
        }

        private void PerformanceOutputHandler(object sendingProcess, System.Diagnostics.DataReceivedEventArgs outLine)
        {
            //* Do your stuff with the output (write to console/log/StringBuilder)
            if (outLine.Data.Length > 0)
            {
                if (outLine.Data.Contains("CPU"))
                    usageSamplesCPU.Add(double.Parse(outLine.Data.Split(' ')[2]));
                else if (outLine.Data.Contains("GPU"))
                    usageSamplesGPU.Add(double.Parse(outLine.Data.Split(' ')[2]));
                else if (outLine.Data.Contains("RAM"))
                    usageSamplesRAM.Add(double.Parse(outLine.Data.Split(' ')[2]));
                else if (outLine.Data.Contains("BW"))
                    usageSamplesBW.Add(double.Parse(outLine.Data.Split(' ')[1]));
            }
        }
    }
}