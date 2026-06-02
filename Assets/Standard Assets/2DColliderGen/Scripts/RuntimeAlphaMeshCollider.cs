#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2)
#define UNITY_4_3_AND_LATER
#endif

#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5)
#define UNITY_4_6_AND_LATER
#endif

#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
#define UNITY_5_AND_LATER
#endif

#if UNITY_4_6_AND_LATER
#define UNITY_SUPPORTS_EDGECOLLIDER2D
#endif

//#define LOG_OUTPUT_IF_EMPTY_REGIONS

#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
#define GENERATE_COLLIDER_IN_SEPARATE_THREAD
#endif

#if GENERATE_COLLIDER_IN_SEPARATE_THREAD
#define USE_THREAD_POOL // comment-out this line if you want to use a separate thread per GameObject with RuntimeAlphaMeshCollider.
#endif

using UnityEngine;
using System.Collections.Generic;
using PixelCloudGames;

//-------------------------------------------------------------------------
/// <summary>
/// A component to generate a collider from an image with alpha channel
/// at runtime.
/// 
/// NOTE: This is experimental code - don't expect it to be perfect yet.
/// </summary>
public class RuntimeAlphaMeshCollider : MonoBehaviour {

    public enum TargetColliderType {
		MeshCollider = 0
#if UNITY_4_3_AND_LATER
		, PolygonCollider2D = 1,
        EdgeCollider2D = 2
#endif
    }

    [System.Flags]
    public enum ProcessingState {
        NothingToDo = 0,
        PrepareForGeneration = 1,
        Generation = 2,
        ApplyResult = 4,

#if GENERATE_COLLIDER_IN_SEPARATE_THREAD
        MainThreadTasks = PrepareForGeneration | ApplyResult,
        ProcessingThreadTasks = Generation
#else
        MainThreadTasks = PrepareForGeneration | Generation | ApplyResult,
        ProcessingThreadTasks = 0
#endif
    }

    protected int mUpdateCounter = 0;

#if UNITY_4_3_AND_LATER
    public TargetColliderType m_ColliderType = TargetColliderType.PolygonCollider2D;
#else
    public TargetColliderType mTargetType = TargetColliderType.MeshCollider;
#endif
	
	public bool m_UseBinaryImageInsteadOfTexture = false; ///< When set to true, the mUsedTexture is ignored and the mBinaryImage attribute is used directly.
	public bool m_OutputColliderInNormalizedSpace = true;
    public Vector2 m_Scale = Vector2.one;
#if UNITY_4_3_AND_LATER
    //public Vector2 m_ScaleForSprite = Vector2.one;
    //protected float m_SpritePixelsPerUnit = 100.0f; // Unity default
    public bool m_AdjustScaleToFitSprite = true;
#endif
    bool m_TransformOutline = false;
    float m_AtlasFrameRotation = 0;
    Vector2 m_AtlasFrameScale = Vector2.one;
    Vector3 m_AtlasFrameOffset = Vector3.zero;

    public Texture2D m_UsedTexture = null;
    public bool m_SetTextureReadable = true;
#if UNITY_4_3_AND_LATER
    public SpriteRenderer m_SpriteRendererForUVRegion = null; ///< Set this parameter if you need to constrain collider generation to the uv region used in the SpriteRenderer.
#endif
    public bool[] m_BinaryImage = null; ///< If you want to set the collider-image directly, set mUseBinaryImageInsteadOfTexture=true and fill this attribute accordingly.
    public int m_BinaryImageWidth;
    public int m_BinaryImageHeight;
    protected int[] m_ClassificationImage;
    protected List<int> m_UsedIndicesWorkList = null;

    public float m_AlphaOpaqueThreshold = 0.1f;
	public int m_MaxNumberOfIslands = 10;
	public int m_MinPixelCountToIncludeIsland = 200;
    public int m_MaxNumberOfHoles = 10;
    public int m_MinPixelCountToIncludeHole = 200;
	public float m_ColliderThickness = 2.0f;
    
    public float m_VertexReductionDistanceTolerance = 0.0f;
	public int m_MaxPointCountPerIsland = 20;
	
	protected PolygonOutlineFromImageFrontend m_OutlineAlgorithm = new PolygonOutlineFromImageFrontend();
	protected IslandDetector m_IslandDetector = new IslandDetector();
	
	protected List<IslandDetector.Region> m_Islands = null;
    protected List<IslandDetector.Region> m_SeaRegions = null;
	protected List<List<Vector2> > m_IntermediateOutlineVerticesAtRegion = new List<List<Vector2> >();
    protected List<List<Vector2> > m_OutlineVerticesAtRegion = new List<List<Vector2> >();

    protected MeshCollider m_MeshCollider = null;
#if UNITY_4_3_AND_LATER
    protected PolygonCollider2D m_PolygonCollider2D = null;
#endif
#if UNITY_SUPPORTS_EDGECOLLIDER2D
    protected EdgeCollider2D[] m_EdgeColliders2D = null;
#endif

#if GENERATE_COLLIDER_IN_SEPARATE_THREAD && !USE_THREAD_POOL
    protected System.Threading.Thread m_Thread = null;
    protected bool m_StopThread = false;
#endif
    public bool m_UpdateColliderOnce = true;
    public bool m_UpdateColliderEveryFrame = false;
    protected ProcessingState m_ProcessingState = ProcessingState.NothingToDo;

    //-------------------------------------------------------------------------
    void Start() {
#if UNITY_4_3_AND_LATER
        if (m_ColliderType == TargetColliderType.MeshCollider) {
#endif

            m_MeshCollider = this.GetComponent<MeshCollider>();
            if (m_MeshCollider == null) {
                m_MeshCollider = this.gameObject.AddComponent<MeshCollider>();
            }
#if UNITY_4_3_AND_LATER
        }
        else if (m_ColliderType == TargetColliderType.PolygonCollider2D) {
            m_PolygonCollider2D = this.GetComponent<PolygonCollider2D>();
            if (m_PolygonCollider2D == null) {
                m_PolygonCollider2D = this.gameObject.AddComponent<PolygonCollider2D>();
            }
        }
        // Note: edge colliders are added when we know how many we need (depends on the number of active collider regions)
#endif
        if (m_UsedTexture == null) {

            if (!m_UseBinaryImageInsteadOfTexture) {
                Debug.LogWarning("Warning: The RuntimeAlphaMeshCollider script is intended to be used with a binary image or writeable texture which is updated at runtime via script. " +
                                 "You should be using a normal AlphaMeshCollider if the texture or sprite never changes.");
            }
#if UNITY_4_3_AND_LATER
            SpriteRenderer spriteRenderer = this.GetComponent<SpriteRenderer>();
            if (spriteRenderer) {
                m_SpriteRendererForUVRegion = spriteRenderer;
                m_UsedTexture = (Texture2D)spriteRenderer.sprite.texture;
            }
            else {
#endif
                Renderer renderer = this.GetComponent<Renderer>();
                m_UsedTexture = (Texture2D)renderer.sharedMaterial.mainTexture;
#if UNITY_4_3_AND_LATER
            }
#endif
        }

#if UNITY_EDITOR
        if (m_SetTextureReadable) {
            SetTextureReadableIfNecessary();
        }
        else {
            WarnIfTextureIsNotReadable();
        }
#endif

#if UNITY_4_3_AND_LATER
        if (m_AdjustScaleToFitSprite) {
            m_OutputColliderInNormalizedSpace = true;
            /*SpriteRenderer spriteRenderer = this.gameObject.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) {
                m_SpritePixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit;
            }*/
        }
#endif
#if GENERATE_COLLIDER_IN_SEPARATE_THREAD && !USE_THREAD_POOL
        m_Thread = new System.Threading.Thread(ProcessingThreadLoop);
        m_Thread.Start();
#endif

        //UpdateColliderIteration(ProcessingState.MainThreadTasks);
	}

    //-------------------------------------------------------------------------
    void SetTextureReadableIfNecessary() {

        if (m_UsedTexture != null) {
            try {
                m_UsedTexture.GetPixels32();
            }
            catch (System.Exception) {
                m_OutlineAlgorithm.SetTextureReadable(m_UsedTexture, true);
            }
        }
    }

    //-------------------------------------------------------------------------
    void WarnIfTextureIsNotReadable() {
        if (m_UsedTexture != null) {
            try {
                m_UsedTexture.GetPixels32();
            }
            catch (System.Exception) {
                Debug.LogWarning("Important performance warning: Set your texture to have Read/Write enabled (set 'Texture Type'='Advanced' to show the parameter), otherwise collider update performance will be very poor!");
            }
        }
    }

#if GENERATE_COLLIDER_IN_SEPARATE_THREAD && !USE_THREAD_POOL
    
    //-------------------------------------------------------------------------
    void OnDestroy() {
        m_StopThread = true;
    }

    void StopThread() {
        m_StopThread = true;
    }

    //-------------------------------------------------------------------------
    void ProcessingThreadLoop() {

        while (!m_StopThread) {
            try {
                UpdateColliderIteration(ProcessingState.ProcessingThreadTasks);
            }
            catch (System.Exception exc) {
                Debug.LogError("Caught exception in processing Thread: " + exc.Message);
            }
        }
    }
#endif

    //-------------------------------------------------------------------------
    void LateUpdate() {

        // Note: this is not recommended, just for demonstration purposes! Usually you might only want to
        // update the collider if anything has changed at the displayed sprite texture.
        if (m_UpdateColliderEveryFrame) {
            if (m_ProcessingState == ProcessingState.NothingToDo) {
                m_UpdateColliderOnce = true;
            }
        }

        if (m_UpdateColliderOnce) {
            m_ProcessingState = ProcessingState.PrepareForGeneration;
            m_UpdateColliderOnce = false;
        }
        
        UpdateColliderIteration(ProcessingState.MainThreadTasks);

#if GENERATE_COLLIDER_IN_SEPARATE_THREAD && USE_THREAD_POOL
        PostColliderIterationThreadPoolTasks(ProcessingState.ProcessingThreadTasks);
#endif
    }


    //-------------------------------------------------------------------------
    /// <summary>
    /// Updates the collider. Call this method from your code accordingly.
    /// </summary>
    public bool UpdateColliderIteration(ProcessingState allowedTasks) {

        bool wasSuccesful = false;
        if (m_ProcessingState == ProcessingState.PrepareForGeneration && (allowedTasks & ProcessingState.PrepareForGeneration) != 0) {
            wasSuccesful = PrepareForGenerationTask();
            m_ProcessingState = ProcessingState.Generation;
        }
        else if (m_ProcessingState == ProcessingState.Generation && (allowedTasks & ProcessingState.Generation) != 0) {
            wasSuccesful = GenerationTask();
            m_ProcessingState = ProcessingState.ApplyResult;
        }
        else if (m_ProcessingState == ProcessingState.ApplyResult && (allowedTasks & ProcessingState.ApplyResult) != 0) {
            wasSuccesful = ApplyResultTask();
            m_ProcessingState = ProcessingState.NothingToDo;
        }
        return wasSuccesful;
    }


#if GENERATE_COLLIDER_IN_SEPARATE_THREAD && USE_THREAD_POOL

    //-------------------------------------------------------------------------
    /// <summary>
    /// Wrapper method providing the necessary "object stateInfo" parameter to fit the requirements to be
    /// passed to ThreadPool.QueueUserWorkItem().
    /// </summary>
    void UpdateColliderIterationThreadPool(object stateInfo) {
        //RuntimeAlphaMeshCollider target = (RuntimeAlphaMeshCollider) stateInfo;
        //target.UpdateColliderIteration(ProcessingState.ProcessingThreadTasks);
        this.UpdateColliderIteration(ProcessingState.ProcessingThreadTasks);
    }

    //-------------------------------------------------------------------------
    /// <summary>
    /// Method to issue necessary threaded calls to the thread-pool.
    /// </summary>
    /// <returns>True if any task needed to be issued to the threadpool.</returns>
    public bool PostColliderIterationThreadPoolTasks(ProcessingState allowedTasks) {
        
        if (m_ProcessingState == ProcessingState.PrepareForGeneration && (allowedTasks & ProcessingState.PrepareForGeneration) != 0) {
            System.Threading.ThreadPool.QueueUserWorkItem(this.UpdateColliderIterationThreadPool);
            return true;
        }
        else if (m_ProcessingState == ProcessingState.Generation && (allowedTasks & ProcessingState.Generation) != 0) {
            System.Threading.ThreadPool.QueueUserWorkItem(this.UpdateColliderIterationThreadPool);
            return true;
        }
        else if (m_ProcessingState == ProcessingState.ApplyResult && (allowedTasks & ProcessingState.ApplyResult) != 0) {
            System.Threading.ThreadPool.QueueUserWorkItem(this.UpdateColliderIterationThreadPool);
            return true;
        }
        return false;
    }
#endif

    //-------------------------------------------------------------------------
    public bool PrepareForGenerationTask() {
        Vector2 regionPositionInPixels = Vector2.zero;
        Vector2 regionSizeinPixels = Vector2.zero;
        bool useRegion = false;
        
#if UNITY_4_3_AND_LATER
        if (m_SpriteRendererForUVRegion) {
            Texture2D texParamToDiscard;
            string frameTitleToDiscard;
            int frameIndexToDiscard;
            
            useRegion = UnityBuiltInSpriteTools.ReadUnity43SpriteParams(m_SpriteRendererForUVRegion, out texParamToDiscard, out frameTitleToDiscard, out frameIndexToDiscard, out regionPositionInPixels, out regionSizeinPixels, out m_AtlasFrameRotation, out m_AtlasFrameScale, out m_AtlasFrameOffset);
            if (m_AtlasFrameRotation != 0 || m_AtlasFrameScale != Vector2.one || m_AtlasFrameOffset != Vector3.one) {
                m_TransformOutline = true;
            }
        }
#endif

        if (!m_UseBinaryImageInsteadOfTexture) {
            bool wasSuccessful = m_OutlineAlgorithm.BinaryAlphaThresholdImageFromTexture(ref m_BinaryImage, out m_BinaryImageWidth, out m_BinaryImageHeight, m_UsedTexture, m_AlphaOpaqueThreshold,
                                                                                        useRegion, (int)regionPositionInPixels.x, (int)regionPositionInPixels.y, (int)regionSizeinPixels.x, (int)regionSizeinPixels.y);
            if (!wasSuccessful) {
                UnityEngine.Debug.LogError(m_OutlineAlgorithm.LastError);
                return false;
            }
        }
        /*     
#if UNITY_4_3_AND_LATER
        if (m_AdjustScaleToFitSprite) {
            Vector2 imageDimensions = new Vector2(mBinaryImage.GetLength(0), mBinaryImage.GetLength(1));
            m_ScaleForSprite = imageDimensions / m_SpritePixelsPerUnit;
        }
#endif*/
        return true;
    }

    //-------------------------------------------------------------------------
    public bool GenerationTask() {
        
        int imageSize = m_BinaryImageWidth * m_BinaryImageHeight;
        if (m_ClassificationImage == null || m_ClassificationImage.Length < imageSize) {
            m_ClassificationImage = new int[imageSize];
        }

        bool anyIslandsFound = CalculateIslandStartingPoints(m_BinaryImage, m_BinaryImageWidth, m_BinaryImageHeight, ref m_ClassificationImage, ref m_Islands, ref m_SeaRegions);
        if (!anyIslandsFound) {

#if LOG_OUTPUT_IF_EMPTY_REGIONS
            UnityEngine.Debug.LogError("Error: No opaque pixel (and thus no island region) has been found in the texture image - is your mAlphaOpaqueThreshold parameter too high?. Stopping collider generation.");
#endif
            return false;
        }

        int numDetectedRegions = m_Islands.Count + m_SeaRegions.Count;
        for (int regionIndex = 0; regionIndex < (Mathf.Min(m_OutlineVerticesAtRegion.Count, numDetectedRegions)); ++regionIndex) {
            if (m_OutlineVerticesAtRegion[regionIndex] == null)
                m_OutlineVerticesAtRegion[regionIndex] = new List<Vector2>();
            else
                m_OutlineVerticesAtRegion[regionIndex].Clear();
        }
        while (m_OutlineVerticesAtRegion.Count < numDetectedRegions) {

            m_OutlineVerticesAtRegion.Add(new List<Vector2>());
        }

        for (int regionIndex = 0; regionIndex < (Mathf.Min(m_IntermediateOutlineVerticesAtRegion.Count, numDetectedRegions)); ++regionIndex) {
            if (m_IntermediateOutlineVerticesAtRegion[regionIndex] == null)
                m_IntermediateOutlineVerticesAtRegion[regionIndex] = new List<Vector2>();
            else
                m_IntermediateOutlineVerticesAtRegion[regionIndex].Clear();
        }
        while (m_IntermediateOutlineVerticesAtRegion.Count < numDetectedRegions) {

            m_IntermediateOutlineVerticesAtRegion.Add(new List<Vector2>());
        }

        m_OutlineAlgorithm.VertexReductionDistanceTolerance = m_VertexReductionDistanceTolerance;
		m_OutlineAlgorithm.MaxPointCount = m_MaxPointCountPerIsland;
		m_OutlineAlgorithm.Convex = false;
		m_OutlineAlgorithm.XOffsetNormalized = -0.5f;
		m_OutlineAlgorithm.YOffsetNormalized = -0.5f;
		m_OutlineAlgorithm.Thickness = m_ColliderThickness;
        
        bool anyIslandVerticesAdded = CalculateOutlineForColliderRegion(true, ref m_OutlineVerticesAtRegion, ref m_IntermediateOutlineVerticesAtRegion, 0, m_Islands, m_BinaryImage, m_BinaryImageWidth, m_BinaryImageHeight, m_MaxNumberOfIslands, m_MinPixelCountToIncludeIsland);
		if (!anyIslandVerticesAdded) {
#if LOG_OUTPUT_IF_EMPTY_REGIONS
            UnityEngine.Debug.LogError("Error: No island vertices added in CalculateUnreducedOutlineForColliderIslands - is your mMinPixelCountToIncludeIsland parameter too low (currently set to " + mMinPixelCountToIncludeIsland + ")?. Stopping collider generation.");
#endif
            return false;
        }

        bool anySeaRegionVerticesAdded = CalculateOutlineForColliderRegion(false, ref m_OutlineVerticesAtRegion, ref m_IntermediateOutlineVerticesAtRegion, m_Islands.Count, m_SeaRegions, m_BinaryImage, m_BinaryImageWidth, m_BinaryImageHeight, m_MaxNumberOfHoles, m_MinPixelCountToIncludeHole);
        if (!anySeaRegionVerticesAdded)
        {
            // normal case.
        }
        
        if (m_TransformOutline) {
#if UNITY_4_3_AND_LATER
            if (!m_AdjustScaleToFitSprite) {
                m_AtlasFrameScale = Vector2.one;
            }
#endif

            for (int index = 0; index < m_OutlineVerticesAtRegion.Count; ++index) {
                TransformVerticesRotPosScale(m_AtlasFrameRotation, m_AtlasFrameScale, m_AtlasFrameOffset, m_OutlineVerticesAtRegion[index]);
            }
        }

        return true;
    }

    //-------------------------------------------------------------------------
    public bool ApplyResultTask() {
        
        bool successful = ApplyReducedOutlineVerticesToCollider();
        return successful;
    }

    void TransformVerticesRotPosScale(float frameRotation, Vector2 outlineScale, Vector3 outlineOffset, List<Vector2> verticesToTransform) {

        // Order of vertex transformation is:
        // 1) rotated by initialRotationQuaternion around transformationCenter
        // 2) scaled by mVertexScaleAfterInitialRotation
        // 3) rotated by mVertexSecondRotationQuaternion around mVertexTransformationCenter
        // 4) scaled by mVertexScaleAfterSecondRotation
        // 3) translated by mVertexOffset

        float scaleX = outlineScale.x;
        float scaleY = outlineScale.y;
        
        Vector3 scaleAfterInitialRotation = new Vector3(scaleX, scaleY, 1);
        //Vector3 scaleAfterSecondRotation = Vector3.one;

        // In order to rotate well, we need to compensate for the gameobject's
        // transform.scale that is applied automatically after all of our transforms.
        //Vector3 automaticallyAppliedScale = this.transform.localScale;
        //Vector3 rotationCompensationScaleBefore = new Vector3(automaticallyAppliedScale.x, automaticallyAppliedScale.y, 1.0f);
        //Vector3 rotationCompensationScaleAfter = new Vector3(1.0f / automaticallyAppliedScale.x, 1.0f / automaticallyAppliedScale.y, 1.0f);
        //scaleAfterInitialRotation.Scale(rotationCompensationScaleBefore);
        //scaleAfterSecondRotation.Scale(rotationCompensationScaleAfter);
        
        Vector3 transformationCenter = new Vector3(0, 0, 0);
        Quaternion initialRotationQuaternion = Quaternion.Euler(0, 0, frameRotation);
        
        for (int index = 0; index < verticesToTransform.Count; ++index) {
            Vector3 inputVertex = new Vector3(verticesToTransform[index].x, verticesToTransform[index].y, 0);
            Vector3 transformedVertex = inputVertex - transformationCenter;
            // rotate initially
            transformedVertex = initialRotationQuaternion * transformedVertex;
            // scale
            transformedVertex.Scale(scaleAfterInitialRotation);
            // rotate a second time
            //transformedVertex = secondRotationQuaternion * transformedVertex;
            // scale 
            //transformedVertex.Scale(scaleAfterSecondRotation);
            // translate
            transformedVertex += outlineOffset;
            transformedVertex += transformationCenter;
            
            verticesToTransform[index] = new Vector2(transformedVertex.x, transformedVertex.y);
        }
    }

    bool ApplyReducedOutlineVerticesToCollider() {

#if UNITY_4_3_AND_LATER
        if (m_ColliderType == TargetColliderType.MeshCollider) {
#endif
            return ApplyReducedOutlineVerticesToMeshCollider();
#if UNITY_4_3_AND_LATER
        }
        else if (m_ColliderType == TargetColliderType.PolygonCollider2D) {
            return ApplyReducedOutlineVerticesToPolygonCollider2D();
        }
#if UNITY_SUPPORTS_EDGECOLLIDER2D
        else if (m_ColliderType == TargetColliderType.EdgeCollider2D) {
            return ApplyReducedOutlineVerticesToEdgeCollider2D();
        }
#endif
        else {
            return false;
        }
#endif  
    }

    //-------------------------------------------------------------------------
    int GetActiveColliderRegionCount() {
        int numActiveIslands = 0;
        for (int islandIndex = 0; islandIndex < m_OutlineVerticesAtRegion.Count; ++islandIndex) {

            List<Vector2> islandOutlineVertices = m_OutlineVerticesAtRegion[islandIndex];
            if (islandOutlineVertices != null && islandOutlineVertices.Count != 0) {
                ++numActiveIslands;
            }
        }
        return numActiveIslands;
    }

#if UNITY_4_3_AND_LATER
    bool ApplyReducedOutlineVerticesToPolygonCollider2D() {

        if (m_PolygonCollider2D == null) {
            return false;
        }
        
        int numActiveIslands = GetActiveColliderRegionCount();
        if (m_PolygonCollider2D.pathCount != numActiveIslands) {
            m_PolygonCollider2D.pathCount = numActiveIslands;
        }

        int activeIslandIndex = 0;
        for (int islandIndex = 0; islandIndex < m_OutlineVerticesAtRegion.Count; ++islandIndex) {

            List<Vector2> islandOutlineVertices = m_OutlineVerticesAtRegion[islandIndex];
            if (islandOutlineVertices != null && islandOutlineVertices.Count != 0) {

                m_PolygonCollider2D.SetPath(activeIslandIndex, islandOutlineVertices.ToArray());
                ++activeIslandIndex;
            }
        }
        return true;
    }
#endif

#if UNITY_SUPPORTS_EDGECOLLIDER2D
    //-------------------------------------------------------------------------
    bool ApplyReducedOutlineVerticesToEdgeCollider2D() {

        int numActiveIslands = GetActiveColliderRegionCount();

        EnsureHasEdgeCollider2DComponents(numActiveIslands);

        int activeIslandIndex = 0;
        for (int islandIndex = 0; islandIndex < m_OutlineVerticesAtRegion.Count; ++islandIndex) {

            List<Vector2> islandOutlineVertices = m_OutlineVerticesAtRegion[islandIndex];
            if (islandOutlineVertices != null && islandOutlineVertices.Count != 0) {

                Vector2[] closedPolygon = new Vector2[islandOutlineVertices.Count + 1]; // + 1 for the looped end vertex
                for (int vertexIndex = 0; vertexIndex < islandOutlineVertices.Count; ++vertexIndex) {
                    closedPolygon[vertexIndex] = islandOutlineVertices[vertexIndex];
                }
                closedPolygon[closedPolygon.Length - 1] = closedPolygon[0];
                m_EdgeColliders2D[activeIslandIndex].points = closedPolygon;
                m_EdgeColliders2D[activeIslandIndex].enabled = true;
                ++activeIslandIndex;
            }
        }
        return true;
    }

    //-------------------------------------------------------------------------
    protected EdgeCollider2D[] EnsureHasEdgeCollider2DComponents(int totalNumIslandCollidersRequired) {

        int numAdditionalCollidersNeeded = 0;
        if (m_EdgeColliders2D == null) {
            numAdditionalCollidersNeeded = totalNumIslandCollidersRequired;
        }
        else {
            numAdditionalCollidersNeeded = totalNumIslandCollidersRequired - m_EdgeColliders2D.Length;
        }

        if (numAdditionalCollidersNeeded > 0) {
            AddEmptyEdgeCollider2DComponents(numAdditionalCollidersNeeded);
            m_EdgeColliders2D = this.GetComponents<EdgeCollider2D>();
        }
        else if (m_EdgeColliders2D.Length > totalNumIslandCollidersRequired) {
            for (int indexToDeactivate = totalNumIslandCollidersRequired; indexToDeactivate < m_EdgeColliders2D.Length; ++indexToDeactivate) {
                m_EdgeColliders2D[indexToDeactivate].enabled = false;
            }
            numAdditionalCollidersNeeded = 0;
        }
        return m_EdgeColliders2D;
    }

    //-------------------------------------------------------------------------
    protected bool AddEmptyEdgeCollider2DComponents(int numIslandCollidersToAdd) {
        if (numIslandCollidersToAdd <= 0) {
            return false;
        }

        for (int count = 0; count < numIslandCollidersToAdd; ++count) {
            /*EdgeCollider2D newCollider = */ this.gameObject.AddComponent<EdgeCollider2D>();
        }
        return true;
    }
#endif

    bool ApplyReducedOutlineVerticesToMeshCollider() {

        if (m_MeshCollider == null) {
            return false;
        }

		Vector3[] fenceVertices;
		int[] fenceTriangleIndices;
		bool isFenceCalculatedSuccessfully = CalculateTriangleFence(out fenceVertices, out fenceTriangleIndices, m_OutlineVerticesAtRegion);
		if (!isFenceCalculatedSuccessfully) {
            UnityEngine.Debug.LogError("Error: Failed to create triangle fence from the outline vertices. Stopping collider generation.");
            return false;
		}
		
		bool isMeshColliderSuccessfullySet = UpdateMeshCollider(fenceVertices, fenceTriangleIndices);
		if (!isMeshColliderSuccessfullySet) {
            UnityEngine.Debug.LogError("Error: Failed to update the mesh collider. Stopping collider generation.");
            return false;
		}
		return true;
	}
	
	//-------------------------------------------------------------------------
    /// <returns>True if at least one island is found, false otherwise.</returns>
	bool CalculateIslandStartingPoints(bool[] binaryImage, int binaryImageWidth, int binaryImageHeight, ref int[] reusedIslandClassificationImage, ref List<IslandDetector.Region> islands, ref List<IslandDetector.Region> seaRegions) {
		
		m_IslandDetector.DetectIslandsFromBinaryImage(binaryImage, m_BinaryImageWidth, m_BinaryImageHeight, ref reusedIslandClassificationImage, ref islands, ref seaRegions);

        return (islands.Count > 0);
	}
	
	//-------------------------------------------------------------------------
	bool CalculateOutlineForColliderRegion(bool isIslandRegion, ref List<List<Vector2> > resultOutlineVerticesAtRegion, ref List<List<Vector2>> intermediateOutlineVerticesAtRegion, int outlineVerticesRegionStartIndex, List<IslandDetector.Region> regions, bool[] binaryImage, int binaryImageWidth, int binaryImageHeight, int maxNumberOfRegions, int minPixelCountToIncludeRegion) {

        int numRegionsUsed = 0;
		for (int regionIndex = 0; regionIndex < regions.Count; ++regionIndex) {

            if (!isIslandRegion && regionIndex == 0) {
                continue; // Note: we skip the first sea region, otherwise we get a rectangle of the image borders.
            }

			IslandDetector.Region region = regions[regionIndex];
			
			if (regionIndex >= maxNumberOfRegions || region.mPointCount < minPixelCountToIncludeRegion) {
				break; // islands are sorted by size already, only smaller islands follow.
			}
			else {
				//List<Vector2> outlineVertices = null;
                bool isCCWOrder = isIslandRegion;
                List<Vector2> intermediateRegionVertices = intermediateOutlineVerticesAtRegion[regionIndex + outlineVerticesRegionStartIndex];
                List<Vector2> resultRegionVertices = resultOutlineVerticesAtRegion[regionIndex + outlineVerticesRegionStartIndex];
                m_OutlineAlgorithm.UnreducedOutlineFromBinaryImage(ref intermediateRegionVertices, binaryImage, m_BinaryImageWidth, m_BinaryImageHeight, region.mPointAtBorder, isIslandRegion, m_OutputColliderInNormalizedSpace, isCCWOrder);

                m_OutlineAlgorithm.ReduceOutline(intermediateRegionVertices, ref resultRegionVertices, ref m_UsedIndicesWorkList, true);
                //TransformVertices(ref outlineVertices);
                //outlineVerticesAtRegion.Add(currentRegionVertices);
                ++numRegionsUsed;
            }
        }
		return numRegionsUsed > 0;
	}

	//-------------------------------------------------------------------------
    /*void TransformVertices(ref List<Vector2> vertices) {

        Vector2 usedScale = mScale;
#if UNITY_4_3_AND_LATER
        usedScale = Vector2.Scale(mScale, m_ScaleForSprite);
#endif

        for (int index = 0; index < vertices.Count; ++index) {
            Vector2 transformedVertex = vertices[index];
            transformedVertex.Scale(usedScale);
            vertices[index] = transformedVertex;
        }
    }*/
	
	//-------------------------------------------------------------------------
	bool CalculateTriangleFence(out Vector3[] jointVertices, out int[] jointTriangleIndices, List<List<Vector2> > outlineVerticesAtIsland) {
		
		List<Vector3[]> islandVertices = new List<Vector3[]>();
		List<int[]> islandTriangleIndices = new List<int[]>();
		
		for (int islandIndex = 0; islandIndex < outlineVerticesAtIsland.Count; ++islandIndex) {
		
			Vector3[] vertices;
			int[] triangleIndices;
			m_OutlineAlgorithm.TriangleFenceFromOutline(out vertices, out triangleIndices, outlineVerticesAtIsland[islandIndex], false);
			islandVertices.Add(vertices);
			islandTriangleIndices.Add(triangleIndices);
		}
		
		JoinVertexGroups(out jointVertices, out jointTriangleIndices, islandVertices, islandTriangleIndices);
		return true;
	}
	
	//-------------------------------------------------------------------------
	bool JoinVertexGroups(out Vector3[] jointVertices, out int[] jointIndices, List<Vector3[]> islandVertices, List<int[]> islandTriangleIndices) {
		
		int numVertices = 0;
		int numIndices = 0;
		int numIslands = islandVertices.Count;
		for (int islandIndex = 0; islandIndex < numIslands; ++islandIndex) {
		
			if (islandVertices[islandIndex] == null || islandTriangleIndices[islandIndex] == null) {
				continue;
			}
			numVertices += islandVertices[islandIndex].Length;
			numIndices += islandTriangleIndices[islandIndex].Length;
		}
		
		jointVertices = new Vector3[numVertices];
		jointIndices = new int[numIndices];
		int jointVertexIndex = 0;
		int jointIndexIndex = 0;
		
		int indexOffset = 0;
		for (int islandIndex = 0; islandIndex < numIslands; ++islandIndex) {
		
			if (islandVertices[islandIndex] == null || islandTriangleIndices[islandIndex] == null) {
				continue;
			}
			
			Vector3[] regionVertices = islandVertices[islandIndex];
			int[] regionIndices = islandTriangleIndices[islandIndex];
			
			for (int regionVertexIndex = 0; regionVertexIndex < regionVertices.Length; ++regionVertexIndex) {
				jointVertices[jointVertexIndex++] = regionVertices[regionVertexIndex];
			}
			for (int islandIndexIndex = 0; islandIndexIndex < regionIndices.Length; ++islandIndexIndex) {
				jointIndices[jointIndexIndex++] = regionIndices[islandIndexIndex] + indexOffset;
			}
			
			indexOffset += regionVertices.Length;
		}
		
		return true;
	}
	
	//-------------------------------------------------------------------------
	bool UpdateMeshCollider(Vector3[] vertices, int[] triangleIndices) {
		
		MeshCollider meshCollider = this.GetComponent<MeshCollider>();
		if (meshCollider == null) {
			this.gameObject.AddComponent<MeshCollider>();
			meshCollider = this.GetComponent<MeshCollider>();
		}
		Mesh colliderMesh = new Mesh();
		colliderMesh.vertices = vertices;
		colliderMesh.triangles = triangleIndices;
		colliderMesh.RecalculateBounds();
		
		meshCollider.sharedMesh = null;
		meshCollider.sharedMesh = colliderMesh;
		return true;
	}
}
