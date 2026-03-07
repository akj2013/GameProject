using System.Collections.Generic;
using UnityEngine;
using WoodLand3D.Resources.Nodes;
using WoodLand3D.Resources.Types;
using WoodLand3D.Gameplay.Player;

namespace WoodLand3D.Gameplay.Interaction
{
    /// <summary>
    /// 자동 채집: 반경 내 가장 가까운 자원을 기준으로 도구를 표시하고,
    /// 채집 애니메이션을 시작한다. 실제 피해·획득·이펙트는 애니메이션 이벤트 시점에서만 처리한다.
    /// </summary>
    /// <remarks>
    /// 흐름: 대상 있음 → 애니만 시작 → 애니 이벤트에서 피해 1회 → 애니 종료 이벤트에서 상태 해제 → 다음 채집 가능.
    /// TODO 수동: 공격/채광 클립에 Animation Event로 OnHarvestHitEvent(타격 프레임), OnHarvestAnimationFinished(끝 프레임) 연결.
    /// </remarks>
    public class PlayerAutoHarvest : MonoBehaviour
    {
        [Header("채집 범위")]
        [SerializeField, Tooltip("채집 가능 반경 (유닛)")]
        private float harvestRadius = 2.5f;

        [SerializeField, Tooltip("애니메이션 타격 이벤트 시 적용할 피해량 (1회당)")]
        private int damagePerHit = 1;

        [Header("툴 표시 (플레이스홀더)")]
        [SerializeField, Tooltip("None일 때는 모두 비활성. 한 번에 하나만 표시")]
        private Transform toolAxe;

        [SerializeField, Tooltip("Pickaxe 오브젝트 (Rock/Ore용)")]
        private Transform toolPickaxe;

        [SerializeField, Tooltip("Sickle 오브젝트 (Rice용)")]
        private Transform toolSickle;

        [SerializeField, Tooltip("Hoe 오브젝트 (Potato/SweetPotato용)")]
        private Transform toolHoe;

        [Header("애니메이션 (선택)")]
        [SerializeField, Tooltip("이동·애니메이션 트리거 담당 (비어 있으면 같은 오브젝트에서 찾음)")]
        private PlayerMover playerMover;

        [SerializeField, Tooltip("채집 상태 판단용 (비어 있으면 같은 오브젝트에서 찾음). Animation Event 없이 종료 감지 시 사용")]
        private Animator animator;

        /// <summary>AC_Player_Base에서 공격/채광 스테이트 이름. 폴백 종료 감지용.</summary>
        private static readonly int StateNameAxe = Animator.StringToHash("Axe");
        private static readonly int StateNamePickAxe = Animator.StringToHash("PickAxe");

        private ToolType _currentTool = ToolType.None;
        private bool _isHarvestAnimating;
        private bool _hasAppliedHitThisSwing;
        private ResourceNode _currentHarvestTarget;

        private void Awake()
        {
            if (playerMover == null)
                playerMover = GetComponent<PlayerMover>();
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        private void Update()
        {
            if (_isHarvestAnimating)
            {
                CancelHarvestAnimationStateIfTargetInvalid();
                TryClearHarvestStateWhenAnimationLeft();
                return;
            }

            TryStartHarvestAnimation();
        }

        /// <summary>
        /// Animation Event가 없어도, Animator가 Axe/PickAxe 스테이트를 벗어나면 채집 상태를 해제한다.
        /// </summary>
        private void TryClearHarvestStateWhenAnimationLeft()
        {
            if (animator == null || !_isHarvestAnimating) return;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool inHarvestState = state.shortNameHash == StateNameAxe || state.shortNameHash == StateNamePickAxe;
            if (!inHarvestState)
                OnHarvestAnimationFinished();
        }

        /// <summary>
        /// 자동 채집 시작 조건이 되면 채집 애니메이션만 시작한다. 피해는 적용하지 않는다.
        /// </summary>
        public void TryStartHarvestAnimation()
        {
            if (_isHarvestAnimating) return;

            var nodesInRange = GatherValidNodesInRange();
            if (nodesInRange.Count == 0)
            {
                SetToolVisibility(ToolType.None);
                return;
            }

            ResourceNode closest = FindClosest(nodesInRange, transform.position);
            if (closest == null || !closest.IsValid) return;

            ToolType requiredTool = ResourceToolMapping.GetToolForResource(closest.Type);
            if (requiredTool != ToolType.Axe && requiredTool != ToolType.Pickaxe)
                return;

            _currentHarvestTarget = closest;
            _hasAppliedHitThisSwing = false;
            _isHarvestAnimating = true;
            SetToolVisibility(requiredTool);
            PlayHarvestAnimationForCurrentTarget(requiredTool);
        }

        /// <summary>
        /// 현재 도구에 맞는 채집 애니메이션(Attack 또는 Mine)을 재생한다.
        /// </summary>
        public void PlayHarvestAnimationForCurrentTarget(ToolType tool)
        {
            if (playerMover == null) return;
            if (tool == ToolType.Axe)
                playerMover.PlayAttackAnimation();
            else if (tool == ToolType.Pickaxe)
                playerMover.PlayMineAnimation();
        }

        /// <summary>
        /// 애니메이션 클립의 타격 프레임에서 Animation Event로 호출한다.
        /// 이 시점에서만 피해 1회 적용 및 획득/이펙트가 처리된다.
        /// </summary>
        public void OnHarvestHitEvent()
        {
            if (_hasAppliedHitThisSwing) return;
            if (_currentHarvestTarget == null || !_currentHarvestTarget.IsValid)
                return;

            _hasAppliedHitThisSwing = true;
            _currentHarvestTarget.ApplyDamage(damagePerHit);
            // 획득·UI·VFX는 ResourceNode.ApplyDamage 고갈 시 기존 플로우(PlayerInventory, ResourceGainedEvents)로 처리됨.
            // 타격 순간 이펙트가 필요하면 여기에 호출 지점 추가 가능.
        }

        /// <summary>
        /// 채집 애니메이션 종료 시 Animation Event 또는 StateMachineBehaviour에서 호출한다.
        /// 다음 채집이 가능하도록 상태를 해제한다.
        /// </summary>
        public void OnHarvestAnimationFinished()
        {
            _isHarvestAnimating = false;
            _hasAppliedHitThisSwing = false;
            _currentHarvestTarget = null;
        }

        /// <summary>
        /// 채집 중인 대상이 사라졌거나 무효해졌을 때 상태만 해제한다.
        /// 애니메이션은 그대로 두고, 다음 Update에서 새 대상을 찾을 수 있게 한다.
        /// </summary>
        public void CancelHarvestAnimationStateIfTargetInvalid()
        {
            if (_currentHarvestTarget == null || !_currentHarvestTarget.IsValid)
            {
                _isHarvestAnimating = false;
                _hasAppliedHitThisSwing = false;
                _currentHarvestTarget = null;
            }
        }

        private List<ResourceNode> GatherValidNodesInRange()
        {
            var list = new List<ResourceNode>();
            var all = Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
            var pos = transform.position;
            float rSq = harvestRadius * harvestRadius;

            foreach (var node in all)
            {
                if (!node.gameObject.activeInHierarchy || !node.IsValid) continue;
                if ((node.transform.position - pos).sqrMagnitude > rSq) continue;
                list.Add(node);
            }

            return list;
        }

        private static ResourceNode FindClosest(List<ResourceNode> nodes, Vector3 from)
        {
            ResourceNode best = null;
            float bestSq = float.MaxValue;

            foreach (var node in nodes)
            {
                float sq = (node.transform.position - from).sqrMagnitude;
                if (sq >= bestSq) continue;
                bestSq = sq;
                best = node;
            }

            return best;
        }

        private void SetToolVisibility(ToolType tool)
        {
            if (_currentTool == tool) return;
            _currentTool = tool;

            SetActive(toolAxe,    tool == ToolType.Axe);
            SetActive(toolPickaxe, tool == ToolType.Pickaxe);
            SetActive(toolSickle, tool == ToolType.Sickle);
            SetActive(toolHoe,    tool == ToolType.Hoe);
        }

        private static void SetActive(Transform t, bool active)
        {
            if (t != null) t.gameObject.SetActive(active);
        }
    }
}

/*
 * ---------- Unity 수동 작업 목록 (애니메이션 타격 시점 기반 채집) ----------
 * 1. 공격 클립(Axe): 타격이 일어나는 프레임에 Animation Event 추가 → 함수명 OnHarvestHitEvent, 인자 없음.
 * 2. 채광 클립(PickAxe): 타격이 일어나는 프레임에 Animation Event 추가 → 함수명 OnHarvestHitEvent, 인자 없음.
 * 3. 두 클립 모두 재생이 끝나는 프레임에 Animation Event 추가 → 함수명 OnHarvestAnimationFinished, 인자 없음.
 * 4. PlayerAutoHarvest가 붙은 오브젝트(Player)를 이벤트 수신 대상으로 두면 됨 (같은 GameObject에 있으므로 함수만 맞추면 호출됨).
 * 5. (대안) StateMachineBehaviour로 Attack/Mine 스테이트 OnStateExit에서 OnHarvestAnimationFinished 호출해도 됨.
 */
