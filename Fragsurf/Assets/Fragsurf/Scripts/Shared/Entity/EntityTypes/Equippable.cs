using Fragsurf.Shared.Packets;
using Fragsurf.Utility;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Fragsurf.Maps;
using SurfaceConfigurator;

namespace Fragsurf.Shared.Entity
{
    public class Equippable : NetEntity, IInteractable, IDamageable
    {

        public Equippable(FSGameLoop game)
            : base(game)
        {
        }

        private string _itemName;
        private int _humanId;
        private bool _equipped;

        [NetProperty]
        public string ItemName
        {
            get { return _itemName; }
            set { SetItemName(value); }
        }
        [NetProperty]
        public int HumanId
        {
            get { return _humanId; }
            set { SetHumanId(value); }
        }
        [NetProperty]
        public bool Equipped
        {
            get { return _equipped; }
            set { SetIsEquipped(value); }
        }
        [NetProperty]
        public int RandomAccumulator
        {
            get { return Random != null ? Random.NumberOfInvokes : 0; }
            set
            {
                if (!Game.IsHost && RandomAccumulator != value)
                {
                    Random = new StateRandom(EntityId, value);
                }
            }
        }
        public StateRandom Random { get; private set; }

        public EquippableGameObject EquippableGameObject => EntityGameObject as EquippableGameObject;
        public Human Human { get; private set; }

        public bool Dead => throw new System.NotImplementedException();

        protected override void _Start()
        {
            Random = new StateRandom(EntityId);
        }

        protected override void _Delete()
        {
            if (Human != null)
            {
                Human.Equippables.Remove(this);
            }

            base._Delete();
        }

        protected override void _Tick()
        {
            base._Tick();

            if(Human == null && _humanId > 0)
            {
                TryFindHuman(_humanId);
            }

            if(Human == null && EquippableGameObject)
            {
                Origin = EquippableGameObject.Position;
                Angles = EquippableGameObject.Rotation;
            }
        }

        private void TryFindHuman(int humanId)
        {
            // EquippableGameObject needs to exist
            if (!EquippableGameObject)
            {
                return;
            }

            Human = Game.EntityManager.FindEntity<Human>(humanId);
            if(Human == null)
            {
                return;
            }

            Human.Equippables.Add(this);
            if (Human.Equippables.Equipped == null)
            {
                Equipped = true;
            }

            var slot = EquippableGameObject.Data.Slot;
            for (int i = Human.Equippables.Items.Count - 1; i >= 0; i--)
            {
                var item = Human.Equippables.Items[i];
                if (item != this
                    && item.EquippableGameObject
                    && item.EquippableGameObject.Data.Slot == slot)
                {
                    if (item.Equipped)
                    {
                        Equipped = true;
                    }
                    item.Drop();
                }
            }
        }

        private void SetItemName(string itemName)
        {
            _itemName = itemName;
            var data = GameData.Instance.Equippables.FirstOrDefault(x => string.Equals(x.Name, itemName, StringComparison.OrdinalIgnoreCase));
            if(data == null)
            {
                if (Game.IsHost)
                {
                    Delete();
                }
                Debug.LogError("Missing Equippable: " + itemName);
                return;
            }
            _itemName = data.Name;
            var obj = Game.NewGameObject();
            var ego = obj.AddComponent(data.ComponentType) as EquippableGameObject;
            ego.Init(this, data);
            EntityGameObject = ego;
            SetIsEquipped(_equipped);
        }

        private void SetHumanId(int humanId)
        {
            _humanId = humanId;

            if (Human != null)
            {
                if (Equipped)
                {
                    Equipped = false;
                }
                Human.Equippables.Remove(this);
                Human = null;
            }

            TryFindHuman(humanId);
        }

        private void SetIsEquipped(bool equipped)
        {
            _equipped = equipped;

            if (!EquippableGameObject)
            {
                return;
            }

            if (equipped)
            {
                if (Human == null)
                {
                    Debug.LogError("Attempted to equip when Human is null!");
                    return;
                }
                Human.Equippables.SetEquippedItem(this);
                EquippableGameObject.ProcessEquip();
            }
            else
            {
                EquippableGameObject.ProcessUnequip();
            }
        }

        public void RunCommand(UserCmd.CmdFields userCmd)
        {
            if (!EquippableGameObject || !_equipped)
            {
                return;
            }

            EquippableGameObject.ProcessRunCommand(userCmd);
        }

        public void Drop()
        {
            if(Human == null)
            {
                Debug.LogError("Attempt to drop item without Human");
                return;
            }

            Human.Equippables.Remove(this);

            if (Equipped)
            {
                var nextItem = Human.Equippables.GetNextItem(EquippableGameObject.Data.Slot) ?? Human.Equippables.GetNextItem();
                if (nextItem != null)
                {
                    nextItem.Equipped = true;
                }
            }

            EquippableGameObject.DropOrigin = Human.HumanGameObject.transform.position + Human.HumanGameObject.transform.forward * .25f + Human.HumanGameObject.transform.right * .25f + Vector3.down * .25f;
            EquippableGameObject.DropAngles = Human.Angles;
            EquippableGameObject.DropForce = Human.Velocity.normalized + Human.HumanGameObject.transform.forward * 1.5f;
            EquippableGameObject.DropTorque = Human.HumanGameObject.transform.forward * 5f;

            HumanId = -1;
        }

        public void OnInteract(NetEntity interactee)
        {
            if(Human == null)
            {
                HumanId = interactee.EntityId;
            }
        }

        public void MouseEnter(int clientIndex)
        {

        }

        public void MouseExit(int clientIndex)
        {

        }

        public void Damage(DamageInfo dmgInfo)
        {
            if(EquippableGameObject.WorldModel.TryGetComponent(out Rigidbody rb))
            {
                var dir = dmgInfo.HitNormal;
                var attacker = Game.EntityManager.FindEntity(dmgInfo.AttackerEntityId);
                if(attacker != null)
                {
                    dir = (Origin - attacker.Origin).normalized;
                }
                rb.AddForceAtPosition(dir * 1.5f, dmgInfo.HitPoint, ForceMode.Impulse);

                if (!Game.IsHost)
                {
                    var effect = Game.Pool.Get(GameData.Instance.GetImpactEffect(SurfaceType.Metal), 1f);
                    effect.transform.position = dmgInfo.HitPoint;
                    effect.transform.forward = dmgInfo.HitNormal;
                }
            }
        }

    }
}

