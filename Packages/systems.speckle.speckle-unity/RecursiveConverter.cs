﻿using Objects.Converter.Unity;
using Speckle.ConnectorUnity.NativeCache;
using Speckle.Core.Kits;
using Speckle.Core.Logging;
using UnityEngine;

namespace Speckle.ConnectorUnity
{
    /// <summary>
    /// <see cref="Component"/> for recursive conversion of Speckle Objects to Native, and Native Objects to Speckle
    /// </summary>
    [AddComponentMenu("Speckle/Conversion/" + nameof(RecursiveConverter))]
    [ExecuteAlways, DisallowMultipleComponent]
    public partial class RecursiveConverter : MonoBehaviour
    {
        public ISpeckleConverter ConverterInstance { get; set; } = new ConverterUnity();

        [field: SerializeField]
        public AggregateNativeCache AssetCache { get; set; }

        private void Awake()
        {
            // Setup.Init(HostApplications.Unity.GetVersion(CoreUtils.GetHostAppVersion()), HostApplications.Unity.Slug);

            if (AssetCache == null)
            {
                var assetCache = ScriptableObject.CreateInstance<AggregateNativeCache>();
                assetCache.nativeCaches = NativeCacheFactory.GetDefaultNativeCacheSetup();
                this.AssetCache = assetCache;
            }
        }
    }
}