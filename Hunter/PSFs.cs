﻿using Newtonsoft.Json;

namespace Hunter
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public enum TaskType
    {
        Action,
        Diagnosis
    }


    public class PerformanceShapingFactor
    {
        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TaskType Type { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("levels")]
        public List<Level> Levels { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("is_static", DefaultValueHandling = DefaultValueHandling.Populate)]
        public bool IsStatic { get; set; } = false;

        [JsonProperty("initial_level")]
        public Level? InitialLevel { get; set; }

        [JsonIgnore]
        public Level CurrentLevel { get; set; }

        public class Level
        {
            [JsonProperty("level")]
            public string LevelName { get; set; }

            [JsonProperty("multiplier")]
            public double Multiplier { get; set; }
        }

        public PerformanceShapingFactor(TaskType type, string label, List<Level> levels, string id, string initialLevelName = null, bool isStatic = false)
        {
            Type = type;
            Label = label;
            Levels = levels;
            Id = id;
            IsStatic = isStatic;

            if (initialLevelName != null)
            {
                CurrentLevel = Levels.FirstOrDefault(l => l.LevelName == initialLevelName);
                if (CurrentLevel == null)
                {
                    throw new ArgumentException("Invalid initial level name.");
                }
            }
            else
            {
                CalculateInitialLevel();
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // Calculate the initial level based on the Levels list
            CalculateInitialLevel();
        }

        public void CalculateInitialLevel()
        {
            foreach (Level level in Levels)
            {
                if (level.LevelName.Contains("Nominal"))
                {
                    CurrentLevel = level;
                    return;
                }
            }
            // If no level with "Nominal" is found, set CurrentLevel to the first level in the list
            CurrentLevel = Levels[0];
        }
        public double CurrentMultiplier
        {
            get
            {
                if (CurrentLevel == null)
                {
                    return 0;
                }
                return Levels.FirstOrDefault(l => l.LevelName == CurrentLevel.LevelName)?.Multiplier ?? 1.0;
            }
        }

        public void Update(TimeSpan? elapsedTime = null, string jsonData = null)
        {
            if (IsStatic)
            {
                return;
            } else
            {
                // Implement effect of lag and linger on PSF levels

                // Adjust PSF based on plant state
            }
        }
    }

    public class PerformanceShapingFactorCollection : IEnumerable<PerformanceShapingFactor>
    {
        private readonly Dictionary<string, PerformanceShapingFactor> _psfs;

        public PerformanceShapingFactorCollection(string filePath = null)
        {
            if (filePath == null)
            {
                string assemblyLocation = Path.GetDirectoryName(typeof(HRAEngine).Assembly.Location);
                filePath = Path.Combine(assemblyLocation, "hunter", "archetypes", "psfs.json");
            }

            string jsonData = File.ReadAllText(filePath);
            List<PerformanceShapingFactor> psfList = JsonConvert.DeserializeObject<List<PerformanceShapingFactor>>(jsonData);

            _psfs = new Dictionary<string, PerformanceShapingFactor>();
            foreach (PerformanceShapingFactor psf in psfList)
            {
                _psfs.Add(psf.Id, psf);
            }
        }

        public PerformanceShapingFactor this[string id]
        {
            get { return _psfs[id]; }
        }

        public int Count
        {
            get { return _psfs.Count; }
        }

        public void Update(TimeSpan? elapsedTime = null, string jsonData = null)
        {
            foreach (var psf in _psfs.Values)
            {
                psf.Update(elapsedTime, jsonData);
            }
        }

        /// <summary>
        /// Calculates the time multiplier for the given task type based on the current 
        /// multipliers of relevant performance shaping factors (PSFs).
        /// </summary>
        /// <param name="taskType">The task type to calculate the time multiplier for.</param>
        /// <returns>The time multiplier for the given task type.</returns>
        public double TimeMultiplier(TaskType taskType)
        {
            // Get all relevant PSFs for the task type
            var relevantPsfs = _psfs.Values.Where(psf => psf.Type == taskType);

            // Filter relevant PSFs further to only include the "Available Time" PSF
            relevantPsfs = relevantPsfs.Where(psf => psf.Label == "Available Time");

            // Calculate the combined multiplier for all relevant PSFs
            double currentMultiplier = relevantPsfs.Aggregate(1.0, (acc, psf) => acc * psf.CurrentMultiplier);

            // If the combined multiplier is greater than 1, return 2.0, otherwise return 1.0
            if (currentMultiplier > 1)
                return 0.5;
            return 1.0;
        }

        /// <summary>
        /// Calculates the composite multiplier for the given task type based on the current 
        /// multipliers of relevant performance shaping factors (PSFs).
        /// </summary>
        /// <param name="taskType">The task type to calculate the composite multiplier for.</param>
        /// <param name="aggregationMethod">The method to use for aggregating the PSF multipliers 
        /// (default is "multiply").</param>
        /// <returns>The composite multiplier for the given task type.</returns>
        public double CompositeMultiplier(TaskType taskType, string aggregationMethod = "multiply")
        {
            // Get all relevant PSFs for the task type
            var relevantPsfs = _psfs.Values.Where(psf => psf.Type == taskType);

            // Aggregate the multipliers based on the specified aggregation method
            switch (aggregationMethod)
            {
                case "multiply":
                    // Multiply the multipliers together
                    return relevantPsfs.Aggregate(1.0, (acc, psf) => acc * psf.CurrentMultiplier);
                case "minimum":
                    // Take the minimum multiplier
                    return relevantPsfs.Min(psf => psf.CurrentMultiplier);
                default:
                    throw new ArgumentException("Invalid aggregation method.");
            }
        }

        public IEnumerator<PerformanceShapingFactor> GetEnumerator()
        {
            return _psfs.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        /// <summary>
        /// Sets the current level of a performance shaping factor with the specified ID to the level with the specified name.
        /// </summary>
        /// <param name="psfId">The ID of the performance shaping factor to update.</param>
        /// <param name="levelName">The name of the level to set as the current level.</param>
        /// <exception cref="ArgumentException">Thrown when the specified performance shaping factor ID or level name is not found.</exception>
        ///
        /// <example>
        /// This example demonstrates how to set the level of two performance shaping factors in a collection:
        ///
        /// <code>
        /// var psfCollection = new PerformanceShapingFactorCollection();
        /// psfCollection.SetLevel("ATa", "Barely adequate time");
        /// psfCollection.SetLevel("ATd", "Barely adequate time");
        /// </code>
        /// </example>
        public void SetLevel(string psfId, string levelName)
        {
            // Check if the performance shaping factor exists in the collection
            if (!_psfs.ContainsKey(psfId))
            {
                throw new ArgumentException($"Performance shaping factor with ID '{psfId}' not found.");
            }

            // Retrieve the performance shaping factor from the collection
            PerformanceShapingFactor psf = _psfs[psfId];

            // Search for the level with the specified name in the performance shaping factor's levels list
            PerformanceShapingFactor.Level level = psf.Levels.FirstOrDefault(l => l.LevelName == levelName);

            // Check if the level was found
            if (level == null)
            {
                throw new ArgumentException($"Level with name '{levelName}' not found for performance shaping factor with ID '{psfId}'.");
            }

            // Set the current level of the performance shaping factor to the specified level
            psf.CurrentLevel = level;
        }

    }
}