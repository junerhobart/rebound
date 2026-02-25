using System.Collections;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace Recall
{
    public class RecallModule : LevelModule
    {
        public float minThrowVelocity = 5.0f;
        public float returnSpeed = 15.0f;

        private Dictionary<Item, RecallData> trackedItems = new Dictionary<Item, RecallData>();

        public override IEnumerator OnLoadCoroutine()
        {
            EventManager.onItemSpawn += OnItemSpawned;
            Debug.Log("[Recall] Module loaded.");
            return base.OnLoadCoroutine();
        }

        public override void OnUnload()
        {
            EventManager.onItemSpawn -= OnItemSpawned;
            foreach (var kvp in trackedItems)
            {
                if (kvp.Key != null)
                    kvp.Key.OnDespawnEvent -= OnItemDespawn;
            }
            trackedItems.Clear();
            base.OnUnload();
        }

        private void OnItemSpawned(Item item)
        {
            if (item.data.type != ItemData.Type.Weapon) return;
            if (trackedItems.ContainsKey(item)) return;

            trackedItems[item] = new RecallData();
            item.OnDespawnEvent += OnItemDespawn;
            item.OnGrabEvent    += (handle, hand)         => OnItemGrabbed(item, handle, hand);
            item.OnUngrabEvent  += (handle, hand, thrown) => OnItemUngrabbed(item, handle, hand, thrown);
        }

        private void OnItemDespawn(EventTime eventTime)
        {
            if (eventTime != EventTime.OnStart) return;
            var item = EventManager.currentItemDespawn;
            if (item != null)
                trackedItems.Remove(item);
        }

        private void OnItemGrabbed(Item item, Handle handle, RagdollHand hand)
        {
            if (trackedItems.TryGetValue(item, out RecallData data))
            {
                data.isReturning = false;
                data.throwingSide = Side.None;
            }
        }

        private void OnItemUngrabbed(Item item, Handle handle, RagdollHand hand, bool thrown)
        {
            if (!trackedItems.TryGetValue(item, out RecallData data)) return;

            if (thrown && item.rb != null && item.rb.velocity.magnitude >= minThrowVelocity)
            {
                data.throwingSide = hand.side;
                data.isReturning  = false;
            }
        }

        public override void Update()
        {
            base.Update();
            CheckRecallInput(Side.Left);
            CheckRecallInput(Side.Right);

            foreach (var kvp in trackedItems)
            {
                if (kvp.Value.isReturning && kvp.Key != null && kvp.Key.rb != null)
                    UpdateReturningItem(kvp.Key, kvp.Value);
            }
        }

        private void CheckRecallInput(Side side)
        {
            var hand = Player.local?.GetHand(side);
            if (hand == null) return;

            if (hand.playerHand.controlHand.gripPressed && hand.ragdollHand.grabbedHandle == null)
                RecallWeapon(side);
        }

        private void RecallWeapon(Side side)
        {
            foreach (var kvp in trackedItems)
            {
                if (kvp.Value.throwingSide == side && !kvp.Value.isReturning
                    && kvp.Key != null && kvp.Key.handlers.Count == 0)
                {
                    kvp.Value.isReturning = true;
                    return;
                }
            }
        }

        private void UpdateReturningItem(Item item, RecallData data)
        {
            var hand = Player.local?.GetHand(data.throwingSide);
            if (hand == null) { data.isReturning = false; return; }

            Vector3 handPos   = hand.ragdollHand.transform.position;
            Vector3 direction = (handPos - item.transform.position).normalized;
            float   distance  = Vector3.Distance(item.transform.position, handPos);

            if (distance < 0.15f && hand.ragdollHand.grabbedHandle == null)
            {
                var handle = item.GetMainHandle(data.throwingSide);
                if (handle != null)
                {
                    hand.ragdollHand.Grab(handle);
                    data.isReturning  = false;
                    data.throwingSide = Side.None;
                    return;
                }
            }

            float speedMult   = Mathf.Clamp(distance * 2.0f, 0.5f, 2.0f);
            Vector3 targetVel = direction * returnSpeed * speedMult;

            item.rb.velocity        = Vector3.Lerp(item.rb.velocity, targetVel, Time.fixedDeltaTime * 5.0f);
            item.rb.angularVelocity = Vector3.Lerp(item.rb.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 3.0f);
        }

        private class RecallData
        {
            public Side throwingSide = Side.None;
            public bool isReturning  = false;
        }
    }
}
