using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Sandbox-only hotbar and status sprites. Campaign leaves this unset and keeps the procedural glyphs.
    /// </summary>
    [CreateAssetMenu(menuName = "Gameplay Prototype/Class UI Icons", fileName = "ClassUiIcons")]
    public class ClassUiIcons : ScriptableObject
    {
        public Sprite arenR;
        public Sprite aren2;
        public Sprite aren3;
        public Sprite aren4;
        public Sprite arenV;
        public Sprite arenC;
        public Sprite bromR;
        public Sprite brom2;
        public Sprite brom3;
        public Sprite brom4;
        public Sprite bromV;
        public Sprite bromC;
        public Sprite lyraR;
        public Sprite lyra2;
        public Sprite lyra3;
        public Sprite lyra4;
        public Sprite lyraV;
        public Sprite lyraC;
        public Sprite seleneR;
        public Sprite selene2;
        public Sprite selene3;
        public Sprite selene4;
        public Sprite seleneV;
        public Sprite seleneC;
        public Sprite vexR;
        public Sprite vex2;
        public Sprite vex3;
        public Sprite vex4;
        public Sprite vexV;
        public Sprite vexC;
        public Sprite stun;
        public Sprite bleed;
        public Sprite slow;
        public Sprite taunt;
        public Sprite absorb;
        public Sprite damageReduction;
        public Sprite haste;
        public Sprite stealth;
        public Sprite dodgeReady;
        public Sprite empowered;
        public Sprite reflect;

        public static ClassUiIcons Active { get; private set; }

        public static void SetActive(ClassUiIcons icons) => Active = icons;

        public Sprite Skill(string classId, SkillId id)
        {
            bool Is(string name) =>
                classId != null && classId.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0;
            Sprite r, a, b, c, v, d;
            if (Is("brom")) { r = bromR; a = brom2; b = brom3; c = brom4; v = bromV; d = bromC; }
            else if (Is("lyra")) { r = lyraR; a = lyra2; b = lyra3; c = lyra4; v = lyraV; d = lyraC; }
            else if (Is("selene")) { r = seleneR; a = selene2; b = selene3; c = selene4; v = seleneV; d = seleneC; }
            else if (Is("vex")) { r = vexR; a = vex2; b = vex3; c = vex4; v = vexV; d = vexC; }
            else { r = arenR; a = aren2; b = aren3; c = aren4; v = arenV; d = arenC; }
            if (id == SkillId.Sword) return r;
            if (id == SkillId.FireDash) return a;
            if (id == SkillId.MultiSlash) return b;
            if (id == SkillId.MawBite) return c;
            if (id == SkillId.Bone) return v;
            if (id == SkillId.Mace) return d;
            return null;
        }
    }
}
