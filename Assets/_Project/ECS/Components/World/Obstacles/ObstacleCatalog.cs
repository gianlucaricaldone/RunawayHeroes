using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Components.Gameplay;

namespace RunawayHeroes.ECS.Components.World.Obstacles
{
    /// <summary>
    /// Catalogo statico di tutti gli ostacoli per tema
    /// </summary>
    public static class ObstacleCatalog
    {
        // Dizionario per il recupero rapido degli ostacoli in base al prefisso e ID
        private static Dictionary<string, ObstacleTypeComponent> _obstaclesByCode = null;
        
        /// <summary>
        /// Inizializza il catalogo se non è già stato fatto
        /// </summary>
        private static void InitializeIfNeeded()
        {
            if (_obstaclesByCode != null)
                return;
                
            _obstaclesByCode = new Dictionary<string, ObstacleTypeComponent>();
            
            // Registra tutti gli ostacoli
            RegisterUniversalObstacles();
            RegisterUrbanObstacles();
            RegisterForestObstacles();
            RegisterTundraObstacles();
            RegisterVolcanoObstacles();
            RegisterAbyssObstacles();
            RegisterVirtualObstacles();
        }
        
        /// <summary>
        /// Ottiene un ostacolo in base al codice (es. "U01", "C03", ecc.)
        /// </summary>
        public static ObstacleTypeComponent GetObstacleByCode(string code)
        {
            InitializeIfNeeded();
            
            if (_obstaclesByCode.TryGetValue(code, out var obstacle))
                return obstacle;
                
            // Se non trovato, restituisce un ostacolo base
            return new ObstacleTypeComponent
            {
                ObstacleID = 0,
                Category = ObstacleCategory.None,
                IsUniversal = true,
                Height = 1.0f,
                Width = 1.0f, 
                Depth = 1.0f,
                DifficultyLevel = 1
            };
        }
        
        /// <summary>
        /// Ottiene ostacoli casuali per un tema specifico, filtrati per livello di difficoltà
        /// </summary>
        public static List<ObstacleTypeComponent> GetRandomObstaclesForTheme(
            WorldTheme theme, 
            int count, 
            int minDifficulty, 
            int maxDifficulty,
            Unity.Mathematics.Random random)
        {
            InitializeIfNeeded();
            
            List<ObstacleTypeComponent> result = new List<ObstacleTypeComponent>();
            List<ObstacleTypeComponent> candidates = new List<ObstacleTypeComponent>();
            
            // Filtra gli ostacoli per tema e difficoltà
            foreach (var obstacle in _obstaclesByCode.Values)
            {
                // Controlla la difficoltà
                if (obstacle.DifficultyLevel < minDifficulty || obstacle.DifficultyLevel > maxDifficulty)
                    continue;
                    
                // Includi sempre gli ostacoli universali
                if (obstacle.IsUniversal)
                {
                    candidates.Add(obstacle);
                    continue;
                }
                
                // Filtra per tema
                switch (theme)
                {
                    case WorldTheme.City:
                        if (obstacle.IsUrbanObstacle)
                            candidates.Add(obstacle);
                        break;
                    case WorldTheme.Forest:
                        if (obstacle.IsForestObstacle)
                            candidates.Add(obstacle);
                        break;
                    case WorldTheme.Tundra:
                        if (obstacle.IsTundraObstacle)
                            candidates.Add(obstacle);
                        break;
                    case WorldTheme.Volcano:
                        if (obstacle.IsVolcanoObstacle)
                            candidates.Add(obstacle);
                        break;
                    case WorldTheme.Abyss:
                        if (obstacle.IsAbyssObstacle)
                            candidates.Add(obstacle);
                        break;
                    case WorldTheme.Virtual:
                        if (obstacle.IsVirtualObstacle)
                            candidates.Add(obstacle);
                        break;
                }
            }
            
            // Se non ci sono candidati, restituisci una lista vuota
            if (candidates.Count == 0)
                return result;
                
            // Seleziona casualmente il numero richiesto di ostacoli
            for (int i = 0; i < count && candidates.Count > 0; i++)
            {
                int index = random.NextInt(0, candidates.Count);
                result.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
            
            return result;
        }
        
        #region Registrazione ostacoli
        
        private static void RegisterUniversalObstacles()
        {
            RegisterObstacle("U01", U01_LowBarrier.GetTypeData());
            RegisterObstacle("U02", U02_HighBarrier.GetTypeData());
            RegisterObstacle("U03", U03_Gap.GetTypeData());
            RegisterObstacle("U04", U04_SideObstacle.GetTypeData());
            RegisterObstacle("U05", U05_HangingObject.GetTypeData());
            RegisterObstacle("U06", U06_DoubleBarrier.GetTypeData());
            RegisterObstacle("U07", U07_FloorSpikes.GetTypeData());
            RegisterObstacle("U08", U08_PowerContainer.GetTypeData());
        }
        
        private static void RegisterUrbanObstacles()
        {
            RegisterObstacle("C01", C01_Car.GetTypeData());
            RegisterObstacle("C02", C02_Van.GetTypeData());
            RegisterObstacle("C03", C03_SurveillanceDrone.GetTypeData());
            RegisterObstacle("C04", C04_PoliceBarricade.GetTypeData());
            RegisterObstacle("C05", C05_FireHydrant.GetTypeData());
            RegisterObstacle("C06", C06_RunawayCar.GetTypeData());
            RegisterObstacle("C07", C07_RoadConstruction.GetTypeData());
            RegisterObstacle("C08", C08_FallenNeonSign.GetTypeData());
        }
        
        private static void RegisterForestObstacles()
        {
            RegisterObstacle("F01", F01_FallenLog.GetTypeData());
            RegisterObstacle("F02", F02_FallenTree.GetTypeData());
            RegisterObstacle("F03", F03_ProtrudingRoots.GetTypeData());
            RegisterObstacle("F04", F04_PoisonousPlant.GetTypeData());
            RegisterObstacle("F05", F05_HangingVines.GetTypeData());
            RegisterObstacle("F06", F06_WaspNest.GetTypeData());
            RegisterObstacle("F07", F07_MudPit.GetTypeData());
            RegisterObstacle("F08", F08_SwingingLog.GetTypeData());
        }
        
        private static void RegisterTundraObstacles()
        {
            RegisterObstacle("T01", T01_IceWall.GetTypeData());
            RegisterObstacle("T02", T02_IceBlock.GetTypeData());
            RegisterObstacle("T03", T03_IcySurface.GetTypeData());
            RegisterObstacle("T04", T04_IceStalactite.GetTypeData());
            RegisterObstacle("T05", T05_FrostGeyser.GetTypeData());
            RegisterObstacle("T06", T06_Avalanche.GetTypeData());
            RegisterObstacle("T07", T07_IceBridge.GetTypeData());
            RegisterObstacle("T08", T08_FreezingWind.GetTypeData());
        }
        
        private static void RegisterVolcanoObstacles()
        {
            RegisterObstacle("V01", V01_LavaPool.GetTypeData());
            RegisterObstacle("V02", V02_LavaGeyser.GetTypeData());
            RegisterObstacle("V03", V03_VolcanicRock.GetTypeData());
            RegisterObstacle("V04", V04_FireWall.GetTypeData());
            RegisterObstacle("V05", V05_VolcanicBomb.GetTypeData());
            RegisterObstacle("V06", V06_VolcanicAsh.GetTypeData());
            RegisterObstacle("V07", V07_PyroclasticFlow.GetTypeData());
            RegisterObstacle("V08", V08_UnstableGround.GetTypeData());
        }
        
        private static void RegisterAbyssObstacles()
        {
            RegisterObstacle("A01", A01_UnderwaterSection.GetTypeData());
            RegisterObstacle("A02", A02_AbyssalCurrent.GetTypeData());
            RegisterObstacle("A03", A03_SwingingTentacles.GetTypeData());
            RegisterObstacle("A04", A04_TanglingSeaweed.GetTypeData());
            RegisterObstacle("A05", A05_AbyssalJellyfish.GetTypeData());
            RegisterObstacle("A06", A06_ToxicGasBubble.GetTypeData());
            RegisterObstacle("A07", A07_AbyssalChasm.GetTypeData());
            RegisterObstacle("A08", A08_InkSpray.GetTypeData());
        }
        
        private static void RegisterVirtualObstacles()
        {
            RegisterObstacle("D01", D01_DataBarrier.GetTypeData());
            RegisterObstacle("D02", D02_Firewall.GetTypeData());
            RegisterObstacle("D03", D03_CorruptionBlock.GetTypeData());
            RegisterObstacle("D04", D04_SpatialGlitch.GetTypeData());
            RegisterObstacle("D05", D05_DataSpike.GetTypeData());
            RegisterObstacle("D06", D06_ActiveVirus.GetTypeData());
            RegisterObstacle("D07", D07_DistortionField.GetTypeData());
            RegisterObstacle("D08", D08_CodeMatrix.GetTypeData());
        }
        
        private static void RegisterObstacle(string code, ObstacleTypeComponent obstacle)
        {
            _obstaclesByCode[code] = obstacle;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Factory per la creazione di ostacoli in base al tipo
    /// </summary>
    public static class ObstacleFactory
    {
        /// <summary>
        /// Crea un'entità ostacolo basata sul codice
        /// </summary>
        public static Entity CreateObstacle(
            EntityCommandBuffer commandBuffer, 
            string obstacleCode, 
            float3 position,
            quaternion rotation = default,
            float scale = 1.0f)
        {
            // Ottieni le informazioni sull'ostacolo dal catalogo
            var obstacleType = ObstacleCatalog.GetObstacleByCode(obstacleCode);
            
            // Crea l'entità di base
            var entity = commandBuffer.CreateEntity();
            
            // Aggiungi il componente di tipo ostacolo
            commandBuffer.AddComponent(entity, obstacleType);
            
            // Aggiungi i componenti specifici in base al tipo di ostacolo
            AddObstacleSpecificComponents(commandBuffer, entity, obstacleCode);
            
            // Aggiungi i tag specifici per tema
            AddThemeSpecificTags(commandBuffer, entity, obstacleType);
            
            // Aggiungi il componente Transform
            commandBuffer.AddComponent(entity, new Unity.Transforms.LocalTransform
            {
                Position = position,
                Rotation = rotation == default ? quaternion.identity : rotation,
                Scale = scale
            });
            
            return entity;
        }
        
        /// <summary>
        /// Aggiunge i componenti specifici in base al codice dell'ostacolo
        /// </summary>
        private static void AddObstacleSpecificComponents(
            EntityCommandBuffer commandBuffer, 
            Entity entity, 
            string obstacleCode)
        {
            var prefix = obstacleCode.Substring(0, 1);
            var id = obstacleCode.Substring(1);
            
            // Aggiungi componenti per ostacoli universali
            if (prefix == "U")
            {
                commandBuffer.AddComponent(entity, new UniversalObstacleTag { ObstacleID = ushort.Parse(id) });
                
                switch (id)
                {
                    case "03": // Gap
                        commandBuffer.AddComponent(entity, new U03_Gap { Width = 2.0f });
                        break;
                    case "04": // Side Obstacle
                        commandBuffer.AddComponent(entity, new U04_SideObstacle { IsLeftSide = false });
                        break;
                    case "05": // Hanging Object
                        commandBuffer.AddComponent(entity, new U05_HangingObject { Height = 2.0f });
                        break;
                    case "08": // Power Container
                        commandBuffer.AddComponent(entity, new U08_PowerContainer { Energy = 50.0f });
                        break;
                }
            }
            // Aggiungi componenti per ostacoli specifici per tema
            else
            {
                switch (prefix)
                {
                    case "C": // City
                        commandBuffer.AddComponent(entity, new UrbanObstacleTag());
                        AddCityObstacleComponents(commandBuffer, entity, id);
                        break;
                    case "F": // Forest
                        commandBuffer.AddComponent(entity, new ForestObstacleTag());
                        AddForestObstacleComponents(commandBuffer, entity, id);
                        break;
                    case "T": // Tundra
                        commandBuffer.AddComponent(entity, new TundraObstacleTag());
                        AddTundraObstacleComponents(commandBuffer, entity, id);
                        break;
                    case "V": // Volcano
                        commandBuffer.AddComponent(entity, new VolcanoObstacleTag());
                        AddVolcanoObstacleComponents(commandBuffer, entity, id);
                        break;
                    case "A": // Abyss
                        commandBuffer.AddComponent(entity, new AbyssObstacleTag());
                        AddAbyssObstacleComponents(commandBuffer, entity, id);
                        break;
                    case "D": // Virtual
                        commandBuffer.AddComponent(entity, new VirtualObstacleTag());
                        AddVirtualObstacleComponents(commandBuffer, entity, id);
                        break;
                }
            }
        }
        
        /// <summary>
        /// Aggiunge i tag specifici per tema dell'ostacolo
        /// </summary>
        private static void AddThemeSpecificTags(
            EntityCommandBuffer commandBuffer, 
            Entity entity, 
            ObstacleTypeComponent obstacleType)
        {
            // Aggiungi tag in base alle proprietà speciali
            if (obstacleType.IsLava)
                commandBuffer.AddComponent(entity, new LavaTag());
                
            if (obstacleType.IsIce)
                commandBuffer.AddComponent(entity, new IceObstacleTag());
                
            if (obstacleType.IsDigital)
                commandBuffer.AddComponent(entity, new DigitalBarrierTag());
                
            if (obstacleType.IsUnderwater)
                commandBuffer.AddComponent(entity, new UnderwaterTag());
                
            if (obstacleType.IsSlippery)
                commandBuffer.AddComponent(entity, new SlipperyTag { SlipFactor = 0.7f });
                
            if (obstacleType.IsToxic)
                commandBuffer.AddComponent(entity, new ToxicGroundTag { ToxicType = 0, DamagePerSecond = 10f });
        }
        
        #region Metodi Helper per Componenti Specifici
        
        private static void AddCityObstacleComponents(
            EntityCommandBuffer commandBuffer, 
            Entity entity, 
            string id)
        {
            switch (id)
            {
                case "01": // Car
                    commandBuffer.AddComponent(entity, new C01_Car { CarType = 0, IsCrashed = true });
                    break;
                case "02": // Van
                    commandBuffer.AddComponent(entity, new C02_Van { IsTipped = false });
                    break;
                case "03": // Surveillance Drone
                    commandBuffer.AddComponent(entity, new C03_SurveillanceDrone 
                    { 
                        MovementSpeed = 3.0f,
                        DetectionRadius = 5.0f
                    });
                    break;
                case "04": // Police Barricade
                    commandBuffer.AddComponent(entity, new C04_PoliceBarricade { HasFlashingLights = true });
                    break;
                case "05": // Fire Hydrant
                    commandBuffer.AddComponent(entity, new C05_FireHydrant 
                    { 
                        WaterForce = 2.0f,
                        IsSpraying = true
                    });
                    break;
                case "06": // Runaway Car
                    commandBuffer.AddComponent(entity, new C06_RunawayCar 
                    { 
                        Speed = 8.0f,
                        VehicleType = 0
                    });
                    break;
                case "07": // Road Construction
                    commandBuffer.AddComponent(entity, new C07_RoadConstruction { ConstructionType = 0 });
                    break;
                case "08": // Fallen Neon Sign
                    commandBuffer.AddComponent(entity, new C08_FallenNeonSign { IsElectrified = true });
                    break;
            }
        }
        
        private static void AddForestObstacleComponents(
            EntityCommandBuffer commandBuffer, 
            Entity entity, 
            string id)
        {
            switch (id)
            {
                case "01": // Fallen Log
                    commandBuffer.AddComponent(entity, new F01_FallenLog 
                    { 
                        Length = 3.5f,
                        RottenLevel = 1
                    });
                    break;
                case "02": // Fallen Tree
                    commandBuffer.AddComponent(entity, new F02_FallenTree { HasBranches = true });
                    break;
                case "03": // Protruding Roots
                    commandBuffer.AddComponent(entity, new F03_ProtrudingRoots { Height = 0.7f });
                    break;
                case "04": // Poisonous Plant
                    commandBuffer.AddComponent(entity, new F04_PoisonousPlant 
                    { 
                        ToxicityLevel = 8.0f,
                        EffectRadius = 2.0f
                    });
                    break;
                case "05": // Hanging Vines
                    commandBuffer.AddComponent(entity, new F05_HangingVines { Length = 3.0f });
                    break;
                case "06": // Wasp Nest
                    commandBuffer.AddComponent(entity, new F06_WaspNest 
                    { 
                        ActivationDistance = 4.0f,
                        WaspCount = 10
                    });
                    break;
                case "07": // Mud Pit
                    commandBuffer.AddComponent(entity, new F07_MudPit { SlowFactor = 0.5f });
                    break;
                case "08": // Swinging Log
                    commandBuffer.AddComponent(entity, new F08_SwingingLog 
                    { 
                        SwingSpeed = 2.0f,
                        SwingRange = 3.0f
                    });
                    break;
            }
        }
        
        private static void AddTundraObstacleComponents(
            EntityCommandBuffer commandBuffer, 
            Entity entity, 
            string id)
        {
            switch (id)
            {
                case "01": // Ice Wall
                    commandBuffer.AddComponent(entity, new T01_IceWall 
                    { 
                        Thickness = 0.5f,
                        Integrity = 80.0f
                    });
                    break;
                case "02": // Ice Block
                    commandBuffer.AddComponent(entity, new T02_IceBlock { IsBreakable = true });
                    break;
                case "03": // Icy Surface
                    commandBuffer.AddComponent(entity, new T03_IcySurface 
                    { 
                        SlipperinessFactor = 0.8f,
                        Length = 5.0f
                    });
                    break;
                case "04": // Ice Stalactite
                    commandBuffer.AddComponent(entity, new T04_IceStalactite 
                    { 
                        FallDelay = 0.5f,
                        TriggerDistance = 3.0f
                    });
                    break;
                case "05": // Frost Geyser
                    commandBuffer.AddComponent(entity, new T05_FrostGeyser 
                    { 
                        EruptionInterval = 3.0f,
                        EruptionDuration = 1.5f,
                        DamagePerSecond = 12.0f
                    });
                    break;
                case "06": // Avalanche
                    commandBuffer.AddComponent(entity, new T06_Avalanche 
                    { 
                        Speed = 10.0f,
                        Width = 6.0f,
                        WarningTime = 1.5f
                    });
                    break;
                case "07": // Ice Bridge
                    commandBuffer.AddComponent(entity, new T07_IceBridge 
                    { 
                        BreakTime = 2.0f,
                        Integrity = 100.0f
                    });
                    break;
                case "08": // Freezing Wind
                    commandBuffer.AddComponent(entity, new T08_FreezingWind 
                    { 
                        Force = 3.0f,
                        Direction = 0.0f,
                        DamagePerSecond = 5.0f
                    });
                    break;
            }
        }
        
        private static void AddVolcanoObstacleComponents(
            EntityCommandBuffer commandBuffer, 
            Entity entity, 
            string id)
        {
            switch (id)
            {
                case "01": // Lava Pool
                    commandBuffer.AddComponent(entity, new V01_LavaPool 
                    { 
                        Temperature = 800.0f,
                        DamagePerSecond = 20.0f
                    });
                    break;
                case "02": // Lava Geyser
                    commandBuffer.AddComponent(entity, new V02_LavaGeyser 
                    { 
                        EruptionInterval = 2.5f,
                        EruptionDuration = 1.0f,
                        Height = 4.0f
                    });
                    break;
                case "03": // Volcanic Rock
                    commandBuffer.AddComponent(entity, new V03_VolcanicRock { IsHot = true });
                    break;
                case "04": // Fire Wall
                    commandBuffer.AddComponent(entity, new V04_FireWall 
                    { 
                        Width = 3.0f,
                        Height = 3.0f,
                        DamagePerSecond = 15.0f
                    });
                    break;
                case "05": // Volcanic Bomb
                    commandBuffer.AddComponent(entity, new V05_VolcanicBomb 
                    { 
                        ExplosionDelay = 3.0f,
                        ExplosionRadius = 4.0f,
                        DamageAmount = 30.0f
                    });
                    break;
                case "06": // Volcanic Ash
                    commandBuffer.AddComponent(entity, new V06_VolcanicAsh 
                    { 
                        Density = 0.7f,
                        Length = 5.0f
                    });
                    break;
                case "07": // Pyroclastic Flow
                    commandBuffer.AddComponent(entity, new V07_PyroclasticFlow 
                    { 
                        Speed = 12.0f,
                        Width = 5.0f,
                        DamagePerSecond = 25.0f
                    });
                    break;
                case "08": // Unstable Ground
                    commandBuffer.AddComponent(entity, new V08_UnstableGround 
                    { 
                        BreakTime = 1.5f,
                        WarningTime = 0.5f
                    });
                    break;
            }
        }
        
        private static void AddAbyssObstacleComponents(
            EntityCommandBuffer commandBuffer, 
            Entity entity, 
            string id)
        {
            switch (id)
            {
                case "01": // Underwater Section
                    commandBuffer.AddComponent(entity, new A01_UnderwaterSection 
                    { 
                        Length = 5.0f,
                        Depth = 3.0f,
                        CurrentStrength = 0.2f
                    });
                    break;
                case "02": // Abyssal Current
                    commandBuffer.AddComponent(entity, new A02_AbyssalCurrent 
                    { 
                        Strength = 3.0f,
                        Direction = 90.0f
                    });
                    break;
                case "03": // Swinging Tentacles
                    commandBuffer.AddComponent(entity, new A03_SwingingTentacles 
                    { 
                        SwingFrequency = 1.0f,
                        SwingAmplitude = 2.0f,
                        TentacleCount = 3
                    });
                    break;
                case "04": // Tangling Seaweed
                    commandBuffer.AddComponent(entity, new A04_TanglingSeaweed 
                    { 
                        SlowFactor = 0.6f,
                        BreakForce = 3.0f
                    });
                    break;
                case "05": // Abyssal Jellyfish
                    commandBuffer.AddComponent(entity, new A05_AbyssalJellyfish 
                    { 
                        StingRadius = 2.0f,
                        DamageAmount = 15.0f,
                        MoveSpeed = 1.5f
                    });
                    break;
                case "06": // Toxic Gas Bubble
                    commandBuffer.AddComponent(entity, new A06_ToxicGasBubble 
                    { 
                        Size = 2.0f,
                        ToxicityLevel = 12.0f,
                        ExpansionRate = 0.5f
                    });
                    break;
                case "07": // Abyssal Chasm
                    commandBuffer.AddComponent(entity, new A07_AbyssalChasm 
                    { 
                        Width = 3.0f,
                        Depth = 10.0f
                    });
                    break;
                case "08": // Ink Spray
                    commandBuffer.AddComponent(entity, new A08_InkSpray 
                    { 
                        BlindnessDuration = 2.0f,
                        BlindnessIntensity = 0.8f,
                        Radius = 3.0f
                    });
                    break;
            }
        }
        
        private static void AddVirtualObstacleComponents(
            EntityCommandBuffer commandBuffer, 
            Entity entity, 
            string id)
        {
            switch (id)
            {
                case "01": // Data Barrier
                    commandBuffer.AddComponent(entity, new D01_DataBarrier 
                    { 
                        Height = 2.5f,
                        Integrity = 100.0f
                    });
                    break;
                case "02": // Firewall
                    commandBuffer.AddComponent(entity, new D02_Firewall 
                    { 
                        DamagePerSecond = 15.0f,
                        Width = 3.0f
                    });
                    break;
                case "03": // Corruption Block
                    commandBuffer.AddComponent(entity, new D03_CorruptionBlock 
                    { 
                        MorphInterval = 3.0f,
                        MorphState = 0
                    });
                    break;
                case "04": // Spatial Glitch
                    commandBuffer.AddComponent(entity, new D04_SpatialGlitch 
                    { 
                        TeleportDistance = 5.0f,
                        TeleportDirection = 0
                    });
                    break;
                case "05": // Data Spike
                    commandBuffer.AddComponent(entity, new D05_DataSpike 
                    { 
                        Height = 1.0f,
                        Damage = 10.0f
                    });
                    break;
                case "06": // Active Virus
                    commandBuffer.AddComponent(entity, new D06_ActiveVirus 
                    { 
                        MoveSpeed = 2.5f,
                        DetectionRadius = 4.0f,
                        DamageAmount = 12.0f
                    });
                    break;
                case "07": // Distortion Field
                    commandBuffer.AddComponent(entity, new D07_DistortionField 
                    { 
                        Duration = 3.0f,
                        Radius = 4.0f
                    });
                    break;
                case "08": // Code Matrix
                    commandBuffer.AddComponent(entity, new D08_CodeMatrix 
                    { 
                        PatternIndex = 0,
                        ShiftInterval = 2.0f
                    });
                    break;
            }
        }
        
        #endregion
    }
}