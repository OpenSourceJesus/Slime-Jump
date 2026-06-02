#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2)
#define UNITY_4_3_AND_LATER
#endif


#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5)
#define UNITY_4_AND_LATER
#endif

#if !(UNITY_3_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
#define UNITY_5_AND_LATER
#endif

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif


#if UNITY_4_3_AND_LATER

namespace PixelCloudGames {

    /// <summary>
    /// A collection of methods to work with Unity's built-in sprites.
    /// </summary>
    public class UnityBuiltInSpriteTools {

        //-------------------------------------------------------------------------
        public static bool ReadUnity43SpriteParams(UnityEngine.SpriteRenderer unity43SpriteRenderer, out Texture2D texture, out string atlasFrameTitle, out int atlasFrameIndex, out Vector2 framePositionInPixels, out Vector2 frameSizeInPixels, out float frameRotation, out Vector2 outlineScale, out Vector3 outlineOffset) {

            texture = null;
            atlasFrameTitle = "";
            atlasFrameIndex = 0;
            framePositionInPixels = frameSizeInPixels = Vector2.zero;
            frameRotation = 0.0f;
            outlineScale = Vector2.one;
            outlineOffset = Vector3.zero;

            UnityEngine.Sprite sprite = unity43SpriteRenderer.sprite;
            if (sprite == null) {
                return false;
            }

            return ReadUnity43SpriteParams(sprite, out texture, out atlasFrameTitle, out atlasFrameIndex, out framePositionInPixels, out frameSizeInPixels, out frameRotation, out outlineScale, out outlineOffset);
        }

        //-------------------------------------------------------------------------
        public static bool ReadUGUIImageSpriteParams(UnityEngine.UI.Image uguiImage, out Texture2D texture, out string atlasFrameTitle, out int atlasFrameIndex, out Vector2 framePositionInPixels, out Vector2 frameSizeInPixels, out float frameRotation, out Vector2 outlineScale, out Vector3 outlineOffset) {

            texture = null;
            atlasFrameTitle = "";
            atlasFrameIndex = 0;
            framePositionInPixels = frameSizeInPixels = Vector2.zero;
            frameRotation = 0.0f;
            outlineScale = Vector2.one;
            outlineOffset = Vector3.zero;

            UnityEngine.Sprite sprite = uguiImage.sprite;
            if (sprite == null) {
                return false;
            }

            bool successfullyReadSprite = ReadUnity43SpriteParams(sprite, out texture, out atlasFrameTitle, out atlasFrameIndex, out framePositionInPixels, out frameSizeInPixels, out frameRotation, out outlineScale, out outlineOffset);
            if (!successfullyReadSprite) {
                return false;
            }

            RectTransform rectTransform = uguiImage.rectTransform;
            Rect rect = rectTransform.rect;
            Vector2 center = rectTransform.rect.center;
            Vector2 size = rect.size;
            outlineOffset = new Vector3(center.x, center.y, 0);
            outlineScale = size;
            return true;
        }

        //-------------------------------------------------------------------------
        public static bool ReadUnity43SpriteParams(UnityEngine.Sprite sprite, out Texture2D texture, out string atlasFrameTitle, out int atlasFrameIndex, out Vector2 framePositionInPixels, out Vector2 frameSizeInPixels, out float frameRotation, out Vector2 outlineScale, out Vector3 outlineOffset) {

            texture = null;
            atlasFrameTitle = "";
            atlasFrameIndex = 0;
            framePositionInPixels = frameSizeInPixels = Vector2.zero;
            frameRotation = 0.0f;
            outlineScale = Vector2.one;
            outlineOffset = Vector3.zero;
            
            texture = sprite.texture;
            atlasFrameTitle = sprite.name;
            UnityEngine.Rect rect = sprite.rect;
            float yPositionBottomTopInvertedOrigin = texture.height - rect.y - rect.height;
            framePositionInPixels = new Vector2(Mathf.Clamp(rect.x, 0, texture.width - 2), // -2 because we want a minimum region size of 1x1 pixels.
                                                Mathf.Clamp(yPositionBottomTopInvertedOrigin, 0, texture.height - 2));

            float remainingWidth = texture.width - framePositionInPixels.x;
            float remainingHeight = texture.height - framePositionInPixels.y; // when counting pixels from the top it's exactly <height> pixels space below.
            frameSizeInPixels = new Vector2(Mathf.Clamp(rect.width, 1, remainingWidth),
                                            Mathf.Clamp(rect.height, 1, remainingHeight));

            outlineScale = new Vector2(sprite.bounds.size.x, sprite.bounds.size.y);

            if (sprite.packed) {
                frameRotation = (sprite.packingRotation == SpritePackingRotation.Any) ? 270.0f : 0.0f;
            }

            float outlineOffsetX = 0.0f;
            float outlineOffsetY = 0.0f;
            // get pivot from bounds
            outlineOffsetX = sprite.bounds.center.x;
            outlineOffsetY = sprite.bounds.center.y;

            outlineOffset = new Vector2(outlineOffsetX, outlineOffsetY);

            return true;
        }

        //-------------------------------------------------------------------------
        public static bool ReadUGUIRawImageParams(UnityEngine.UI.RawImage uguiImage, out Texture2D texture, out string atlasFrameTitle, out int atlasFrameIndex, out Vector2 framePositionInPixels, out Vector2 frameSizeInPixels, out float frameRotation, out Vector2 outlineScale, out Vector3 outlineOffset)
        {
            bool isAtlasUsed = false;
            texture = null;
            atlasFrameTitle = "";
            atlasFrameIndex = 0;
            framePositionInPixels = frameSizeInPixels = Vector2.zero;
            frameRotation = 0.0f;
            outlineScale = Vector2.one;
            outlineOffset = Vector3.zero;

            texture = (Texture2D) uguiImage.texture;
            if (texture == null)
            {
                return isAtlasUsed;
            }
                        
            RectTransform rectTransform = uguiImage.rectTransform;
            Rect rect = rectTransform.rect;
            Vector2 center = rectTransform.rect.center;
            Vector2 size = rect.size;
            outlineOffset = new Vector3(center.x, center.y, 0);
            outlineScale = size;
            return isAtlasUsed;
        }


#if UNITY_EDITOR
        //-------------------------------------------------------------------------
        public static bool ReadUnity43SpriteAnimatorParams(GameObject gameObject, UnityEngine.Animator unity43Animator, ref Sprite[] spriteFramesArray, ref string[] spriteIDStrings, ref int numFrames) {

#if UNITY_5_AND_LATER
            UnityEditor.Animations.AnimatorController controller = (UnityEditor.Animations.AnimatorController) unity43Animator.runtimeAnimatorController;
#else
            UnityEditorInternal.AnimatorController controller = (UnityEditorInternal.AnimatorController) unity43Animator.runtimeAnimatorController;
#endif
            if (!controller) {
                numFrames = 1;
                return false;
            }
            // Note: we have to maintain the order to keep the spriteRef and spriteID indices linked.
            // Thus we don't use a HashSet for duplicate prevention but a list.Contains() call.
            List<string> spriteIDs = new List<string>();
            List<UnityEngine.Sprite> spriteFrames = new List<Sprite>();

            AnimationClip[] clips = AnimationUtility.GetAnimationClips(gameObject);
            for (int index = 0; index < clips.Length; ++index) {
                AnimationClip clip = clips[index];

                EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                if (bindings != null) {
                    for (int bindingIndex = 0; bindingIndex < bindings.Length; ++bindingIndex) {
                        EditorCurveBinding binding = bindings[bindingIndex];
                        if (binding.isPPtrCurve) {
                            ObjectReferenceKeyframe[] references = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                            for (int refIndex = 0; refIndex < references.Length; ++refIndex) {
                                ObjectReferenceKeyframe reference = references[refIndex];
                                UnityEngine.Sprite spriteRef = (UnityEngine.Sprite) reference.value;
                                if (spriteRef != null) {
                                    string spriteID = spriteRef.name;
                                    if (!spriteIDs.Contains(spriteID)) {
                                        spriteFrames.Add(spriteRef);
                                        spriteIDs.Add(spriteID);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            spriteFramesArray = spriteFrames.ToArray();
            spriteIDStrings = spriteIDs.ToArray();
            numFrames = spriteIDStrings.Length;
            return true;
        }
#endif // UNITY_EDITOR

    } // end class
} // end namespace

#endif // UNITY_4_3_AND_LATER
