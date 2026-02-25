using ThunderRoad;
using UnityEngine;

namespace Rebound
{
    public class ReboundScript : ThunderScript
    {
        // ── Mod options ───────────────────────────────────────────────────────────

        [ModOption("Min Throw Velocity")]
        [ModOptionTooltip("Minimum throw speed (m/s) before a thrown item can be recalled.")]
        [ModOptionOrder(1)]
        [ModOptionFloatValues(1f, 15f, 0.5f)]
        [ModOptionSlider]
        [ModOptionSave]
        public static float minThrowVelocity = 4f;

        [ModOption("Return Speed")]
        [ModOptionTooltip("How fast recalled items fly back to your hand.")]
        [ModOptionOrder(2)]
        [ModOptionFloatValues(5f, 40f, 1f)]
        [ModOptionSlider]
        [ModOptionSave]
        public static float returnSpeed = 18f;

        [ModOption("Arc Strength")]
        [ModOptionTooltip("How much the item arcs overhead on its way back. 0 = straight line.")]
        [ModOptionOrder(3)]
        [ModOptionFloatValues(0f, 0.15f, 0.03f)]
        [ModOptionArrows]
        [ModOptionSave]
        public static float arcStrength = 0.06f;

        [ModOption("Weighted Feel")]
        [ModOptionTooltip("1 = heavier items travel slightly slower. 0 = all items same speed.")]
        [ModOptionOrder(4)]
        [ModOptionFloatValues(0f, 1f, 1f)]
        [ModOptionArrows]
        [ModOptionSave]
        public static float weightedFeel = 1f;

        // ── Single tracked item ───────────────────────────────────────────────────

        private Item       trackedItem = null;
        private RecallData trackedData = null;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        public override void ScriptLoaded(ModManager.ModData modData)
        {
            base.ScriptLoaded(modData);
            EventManager.OnItemGrab    += OnItemGrabbed;
            EventManager.OnItemRelease += OnItemReleased;
            EventManager.onLevelUnload += OnLevelUnload;
            Debug.Log("[Rebound] Loaded.");
        }

        public override void ScriptUnload()
        {
            EventManager.OnItemGrab    -= OnItemGrabbed;
            EventManager.OnItemRelease -= OnItemReleased;
            EventManager.onLevelUnload -= OnLevelUnload;
            AbandonTracked();
            base.ScriptUnload();
        }

        private void OnLevelUnload(LevelData levelData, LevelData.Mode mode, EventTime eventTime)
        {
            if (eventTime == EventTime.OnStart)
                AbandonTracked();
        }

        // ── Item tracking ─────────────────────────────────────────────────────────

        private void OnItemGrabbed(Handle handle, RagdollHand ragdollHand)
        {
            // If the tracked item is picked up by anyone, clear tracking
            if (handle?.item != null && handle.item == trackedItem)
                AbandonTracked();
        }

        private void OnItemReleased(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            var item = handle?.item;
            if (item == null) return;

            var rb = handle.physicBody?.rigidBody;
            if (!throwing || rb == null || rb.velocity.magnitude < minThrowVelocity) return;

            // Abandon whatever was tracked before — one item at a time
            AbandonTracked();

            trackedItem = item;
            trackedData = new RecallData
            {
                handle       = handle,
                savedGravity = handle.physicBody.useGravity,
            };
        }

        // ── Update loop ───────────────────────────────────────────────────────────

        public override void ScriptUpdate()
        {
            if (Player.local == null) return;

            // Either hand can trigger recall
            CheckRecallInput(Side.Left);
            CheckRecallInput(Side.Right);

            if (trackedItem != null && trackedData != null && trackedData.isReturning)
            {
                if (trackedItem == null)
                    AbandonTracked();
                else
                    UpdateReturningItem();
            }
        }

        private void CheckRecallInput(Side side)
        {
            if (trackedItem == null || trackedData == null) return;
            if (trackedData.isReturning) return;
            if (trackedData.handle == null || trackedData.handle.handlers.Count != 0) return;

            var hand = Player.local.GetHand(side);
            if (hand == null) return;
            if (!hand.controlHand.gripPressed || hand.ragdollHand.grabbedHandle != null) return;

            // Start returning to whichever hand pressed grip
            trackedData.returnToSide = side;
            trackedData.isReturning  = true;

            var pb = trackedData.handle.physicBody;
            if (pb != null)
            {
                pb.useGravity = false;

                var rb = pb.rigidBody;
                if (rb != null)
                {
                    // Mjolnir spin — orientation control will straighten it on approach
                    Vector3 spinAxis = Vector3.Cross(rb.velocity.normalized, Vector3.up);
                    if (spinAxis.sqrMagnitude < 0.01f) spinAxis = trackedItem.transform.right;
                    rb.angularVelocity = spinAxis.normalized * 14f;
                }
            }
        }

        private void UpdateReturningItem()
        {
            var hand = Player.local.GetHand(trackedData.returnToSide);
            if (hand == null) { AbandonTracked(); return; }

            var pb = trackedData.handle?.physicBody;
            var rb = pb?.rigidBody;
            if (rb == null) { AbandonTracked(); return; }

            Vector3 handPos  = hand.ragdollHand.transform.position;
            Vector3 toHand   = handPos - trackedItem.transform.position;
            float   distance = toHand.magnitude;
            Vector3 dir      = toHand.normalized;

            // ── Auto-catch ────────────────────────────────────────────────────────
            if (distance < 0.2f && hand.ragdollHand.grabbedHandle == null)
            {
                var handle = trackedData.handle ?? trackedItem.GetMainHandle(trackedData.returnToSide);
                if (handle != null)
                {
                    RestoreGravity();

                    // Position item exactly at hand before grabbing — kills the teleport particle
                    rb.velocity        = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    trackedItem.transform.position = handPos;

                    bool prevSilent   = handle.silentGrab;
                    handle.silentGrab = true;
                    hand.ragdollHand.Grab(handle, false, false);
                    handle.silentGrab = prevSilent;

                    hand.controlHand.HapticShort(0.6f, false);

                    // OnItemGrabbed will fire and call AbandonTracked — nothing else needed
                    return;
                }
            }

            // ── Arc ───────────────────────────────────────────────────────────────
            float arcT      = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((distance - 0.3f) / 2.0f))
                            * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((6.0f - distance) / 4.0f));
            Vector3 finalDir = (dir + Vector3.up * arcT * arcStrength).normalized;

            // ── Speed ─────────────────────────────────────────────────────────────
            // Weighted feel only scales top speed, NOT steering response.
            // Fixed high lerpSpeed ensures heavy items track reliably.
            float mass     = weightedFeel >= 0.5f ? Mathf.Max(rb.mass, 0.1f) : 1.0f;
            float inertia  = 1.0f + Mathf.Log(1.0f + mass);   // ~1.1 light → ~3 heavy
            float topSpeed = distance < 0.4f
                ? returnSpeed * 0.5f
                : returnSpeed * Mathf.Clamp(1f + distance * 0.1f, 1f, 2f) / Mathf.Sqrt(inertia * 0.5f);

            // High fixed lerp rate — reliable for all masses (was heavy-item bug cause)
            rb.velocity = Vector3.Lerp(rb.velocity, finalDir * topSpeed, Time.deltaTime * 30f);

            // ── Handle orientation ────────────────────────────────────────────────
            Vector3 handleOffset = trackedData.handle.transform.position - trackedItem.transform.position;
            // Negate so the blade end (opposite of handle) points away — handle arrives at player's hand
            Vector3 gripDir      = handleOffset.sqrMagnitude > 0.001f
                ? -handleOffset.normalized
                : -trackedData.handle.transform.up;

            float orientBlend = Mathf.Clamp01(1f - distance / 5f);
            float orientSpeed = Mathf.Lerp(4f, 14f, orientBlend); // fixed — no inertia division

            Quaternion deltaRot = Quaternion.FromToRotation(gripDir, dir);
            deltaRot.ToAngleAxis(out float angleDeg, out Vector3 rotAxis);
            if (angleDeg > 180f) angleDeg -= 360f;

            rb.angularVelocity = Vector3.Lerp(
                rb.angularVelocity,
                rotAxis.normalized * (angleDeg * Mathf.Deg2Rad * orientSpeed),
                Time.deltaTime * orientSpeed);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void AbandonTracked()
        {
            if (trackedData != null) RestoreGravity();
            trackedItem = null;
            trackedData = null;
        }

        private void RestoreGravity()
        {
            var pb = trackedData?.handle?.physicBody;
            if (pb != null && pb.rigidBody != null)
                pb.useGravity = trackedData.savedGravity;
        }

        private class RecallData
        {
            public Side   returnToSide  = Side.Right;
            public bool   isReturning   = false;
            public Handle handle        = null;
            public bool   savedGravity  = true;
        }
    }
}
