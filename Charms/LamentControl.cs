using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using SFCore.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vasi;
using Random = UnityEngine.Random;

namespace FiveKnights
{
    internal class LamentControl : MonoBehaviour
    {
        public List<GameObject> markedEnemies = new List<GameObject>();
        private PlayMakerFSM _spellControl;
        private PlayerData _pd = PlayerData.instance;
        private HeroController _hc = HeroController.instance;
        private PlayMakerFSM _pvControl;
        private List<GameObject> _blast = new List<GameObject>();
        private List<GameObject> _line = new List<GameObject>();
        private List<GameObject> _focusLines = new List<GameObject>();
        private List<IEnumerator> _createLine = new List<IEnumerator>();
        private void OnEnable()
        {
            On.HealthManager.TakeDamage += ApplyStatus;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ClearList;

            _pvControl = _hc.gameObject.Find("HK Prime(Clone)(Clone)").LocateMyFSM("Control");

            _spellControl = HeroController.instance.spellControl;
            if (_spellControl != null)
            {
                _spellControl.InsertMethod("Cancel All", 33, BlastControlCancel);
                _spellControl.InsertMethod("Focus Cancel", 15, BlastControlCancel);
                _spellControl.InsertMethod("Focus Cancel 2", 18, BlastControlCancel);

                _spellControl.InsertMethod("Focus Blast", 15, BlastControlFadeIn);
                _spellControl.InsertMethod("Start MP Drain Blast", 2, BlastControlFadeIn);

                _spellControl.InsertMethod("Focus Heal Blast", 16, BlastControlMain);
                _spellControl.InsertMethod("Focus Heal 2 Blast", 18, BlastControlMain);
            }
        }
        private void BlastControlCancel()
        {
            PureVesselBlastCancel();
        }
        private void PureVesselBlastCancel()
        {
            foreach (GameObject enemy in markedEnemies)
            {
                Log("Called CancelBlast");
                var index = markedEnemies.IndexOf(enemy);
               try
                {
                    if(_focusLines != null) { 
                    Log("Attempting to stop focus lines");
                    _focusLines[index].GetComponent<tk2dSpriteAnimator>().Stop();
                    Log("Animation was stopped");
                    Destroy(_focusLines[index]);
                    Log("The object was deleted");
                    _focusLines.RemoveAt(index);
                    Log($"The index number {index} was cleared");
                        }
                }
                catch(ArgumentOutOfRangeException e) { }
                try
                {
                    if (markedEnemies[index] != null)
                    {
                        if (!markedEnemies[index].GetComponent<Afflicted>())
                        {
                            Log("Afficted component not found");
                        }
                        else
                        {
                            markedEnemies[index].GetComponent<Afflicted>().SoulEffect.SetActive(true);
                            markedEnemies[index].GetComponent<Afflicted>().SoulEffect.GetComponent<ParticleSystem>().Play();
                            Log("Reactivated Soul Effeect");
                        }
                    }
                }
                catch (ArgumentOutOfRangeException e) { Log("Exception caught in soul effect"); }

                try
                {
                    StopCoroutine(_createLine[index]);
                    if (_line[index] != null)
                    {
                        Destroy(_line[index]);
                    }
                    _createLine.RemoveAt(index);
                    Log("Removed line");                       
                }
                catch (ArgumentOutOfRangeException e) {Log("Exception caught in line"); }
                try
                {

                        Destroy(_blast[index]);
                        _blast.RemoveAt(index);
                        Log("Removed blast");
                    
                }
                catch (ArgumentOutOfRangeException e) { Log("Exception caught in blast"); }
                //_hc.gameObject.GetComponent<LamentControl>()._audio.Stop();
                Log("Removed Blast Object");

            }

        }
        private void BlastControlFadeIn ()
        {
            for(int i = 0; i < markedEnemies.Count; i++)
            {
                GameObject enemy = markedEnemies[i];
                for(int j = 0; j < markedEnemies.Count; j++)
                {
                    GameObject compare = markedEnemies[j];
                    if(i == j) continue;
                    if(compare != enemy) continue;

                    Log("Removed Duplicate Object");
                    Log($"{compare} was in the list twice");
                    markedEnemies.Remove(compare);
                }
                Log("Start coroutine: FadeIn");
                Log("Enemy index: " + markedEnemies.IndexOf(enemy));
                if (enemy == null)
                {
                    markedEnemies.RemoveAt(i);
                    Log("Removed null entity");
                    i--;
                    continue;
                }
                StartCoroutine(PureVesselBlastFadeIn(enemy));
            }
        }
        private void BlastControlMain()
        {
            foreach (GameObject enemy in markedEnemies)
            {
                Log("Start Coroutine: Blast");
                var index = markedEnemies.IndexOf(enemy);
                if (enemy == null)
                {
                    try
                    {
                        if (_line[index] != null) Destroy(_line[index]);
                        if (_blast[index] == null) _line.RemoveAt(index);
                    }
                    catch (ArgumentOutOfRangeException e) { }
                    try
                    {
                        if (_blast[index] != null) Destroy(_blast[index]);
                        if (_blast[index] == null) _blast.RemoveAt(index);
                    }
                    catch (ArgumentOutOfRangeException e) { }
                    markedEnemies.RemoveAt(index);
                    Log("Removed null entity");
                    continue;
                }
                StartCoroutine(PureVesselBlast(enemy));
            }

        }
        private void OnDisable()
        {
            On.HealthManager.TakeDamage -= ApplyStatus;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= ClearList;

            if (_spellControl != null)
            {

                _spellControl.RemoveAction("Cancel All", 33);
                _spellControl.RemoveAction("Focus Cancel", 15);
                _spellControl.RemoveAction("Focus Cancel 2", 18);

                _spellControl.RemoveAction("Focus Blast", 15);
                _spellControl.RemoveAction("Start MP Drain Blast", 2);

                _spellControl.RemoveAction("Focus Heal Blast", 16);
                _spellControl.RemoveAction("Focus Heal 2 Blast", 18);
            }

        }

        private void ClearList(Scene PrevScene, Scene NextScene)
        {
            markedEnemies.Clear();
            _focusLines.Clear();
            _line.Clear();
            _blast.Clear();
            foreach (GameObject go in markedEnemies)
            {
                Log($"This should be empty but {go} is still there");
            }
            Log("Cleared list");
        }

        private void ApplyStatus(On.HealthManager.orig_TakeDamage orig, HealthManager self, HitInstance hitInstance)
        {
            orig(self, hitInstance);
            if (hitInstance.AttackType == AttackTypes.Nail)
            {
                if (self.gameObject.GetComponent<Afflicted>() == null)
                {

                    self.gameObject.AddComponent<Afflicted>();
                    markedEnemies.Add(self.gameObject);
                    foreach (GameObject go in markedEnemies)
                    {
                        Log(go + " in list");
                    }
                }
            }
        }
        private IEnumerator PureVesselBlastFadeIn(GameObject enemy)
        {
            Log("Called PureVesselBlastFadeIn");
            Log("Recieved GO: " + enemy);
            var index = markedEnemies.IndexOf(enemy);
            Log("Blast Index: " + index);

            if (markedEnemies[index].GetComponent<Afflicted>() != null) { StartCoroutine(markedEnemies[index].GetComponent<Afflicted>().FadeOut()); }

            _createLine.Insert(index, CreateLine(index, enemy, enemy.transform.position));
            StartCoroutine(_createLine[index]);
            _focusLines.Insert(index, Instantiate(_hc.gameObject.Find("Focus Effects").Find("Lines Anim"), enemy.transform.position, new Quaternion(0, 0, 0, 0)));
            _focusLines[index].GetComponent<tk2dSpriteAnimator>().Play("Focus Effect");
       

            this.PlayAudio((AudioClip)_pvControl.GetAction<AudioPlayerOneShotSingle>("Focus Charge", 2).audioClip.Value, 1.2f, 1.5f);
            var blast = Instantiate(FiveKnights.preloadedGO["Blast"]);
            _blast.Insert(index, blast);
            _blast[index].transform.position += markedEnemies[index].transform.position;
            _blast[index].SetActive(true);
            Destroy(_blast[index].FindGameObjectInChildren("hero_damager"));

            if (_pd.GetBool("equippedCharm_" + Charms.DeepFocus))
            {
                _blast[index].transform.localScale *= 2.5f;
            }
            else
            {
                _blast[index].transform.localScale *= 1.5f;
            }

            Animator anim = _blast[index].GetComponent<Animator>();
            anim.speed = 1;
            if (_pd.GetBool("equippedCharm_" + Charms.QuickFocus))
            {
                anim.speed *= 1.5f;
            }

            if (_pd.GetBool("equippedCharm_" + Charms.DeepFocus))
            {
                anim.speed -= anim.speed * 0.35f;
            }
            yield return null;
            Log("Fade in finished");
        }

        private IEnumerator CreateLine(int index, GameObject enemy, Vector3 enemypos)
        {
            var wait = 1f;
            if (_pd.GetBool("equippedCharm_" + Charms.QuickFocus))
            {
                wait *= 1.5f;
            }
            if (_pd.GetBool("equippedCharm_" + Charms.DeepFocus))
            {
                wait -= wait * 0.35f;
            }
            yield return new WaitForSeconds(wait - .2f);
            var heropos = gameObject.transform.position - new Vector3(0, 1, 0);

            var linepos = Vector3.Lerp(heropos, enemypos, .5f);     
            
            float num = heropos.y - enemypos.y;
            float num2 = heropos.x - enemypos.x;
            float lineangle;
            for (lineangle = Mathf.Atan2(num, num2) * (180f / (float)Math.PI); lineangle < 0f; lineangle += 360f)
            {
            }
            Log(lineangle);
            var linesize = Vector2.Distance(heropos, enemypos);

            _line.Insert(index, Instantiate(FiveKnights.preloadedGO["SoulTwister"].LocateMyFSM("Mage").GetAction<CreateObject>("Tele Line").gameObject.Value, linepos, new Quaternion(0, 0, 0, 0)));
            _line[index].transform.SetRotationZ(lineangle);
            _line[index].transform.localScale = new Vector3(linesize, 1, 1);
            _line[index].GetComponent<ParticleSystem>().loop = true;
            _line[index].GetComponent<ParticleSystem>().startSize = .35f;
            _line[index].GetComponent<ParticleSystem>().Emit(0);
            _line[index].SetActive(true);
            _line[index].GetComponent<ParticleSystem>().Play();
            yield return new WaitForSeconds(.075f);
            _line[index].GetComponent<ParticleSystem>().loop = false;
        }
        private IEnumerator PureVesselBlast(GameObject enemy)
        {
            Log("Called PureVesselBlast");
            Log("Recieved GO: " + enemy);
            var index = markedEnemies.IndexOf(enemy);
            _focusLines[index].GetComponent<tk2dSpriteAnimator>().Play("Focus Effect End");
            _blast[index].layer = 17;
            Animator anim = _blast[index].GetComponent<Animator>();
            anim.speed = 1;
            int hash = anim.GetCurrentAnimatorStateInfo(0).fullPathHash;
            anim.PlayInFixedTime(hash, -1, 0.8f);

            Log("Adding CircleCollider2D");            
            CircleCollider2D blastCollider = _blast[index].AddComponent<CircleCollider2D>();
            blastCollider.radius = 2.5f;
            if (_pd.GetBool("equippedCharm_" + Charms.DeepFocus))
            {
                blastCollider.radius *= 2.5f;
            }
            else
            {
                blastCollider.radius *= 1.5f;
            }

            blastCollider.offset = Vector3.down;
            blastCollider.isTrigger = true;
            Log("Adding DebugColliders");
            //_blast.AddComponent<DebugColliders>();
            Log("Adding DamageEnemies");
            _blast[index].AddComponent<DamageEnemies>();
            DamageEnemies damageEnemies = _blast[index].GetComponent<DamageEnemies>();
            damageEnemies.damageDealt = 30;
            damageEnemies.attackType = AttackTypes.Spell;
            damageEnemies.ignoreInvuln = false;
            damageEnemies.enabled = true;
            Log("Playing AudioClip");
            this.PlayAudio((AudioClip)_pvControl.GetAction<AudioPlayerOneShotSingle>("Focus Burst", 8).audioClip.Value, 1.5f, 1.5f);
            Log("Audio Clip finished");
            yield return new WaitForSeconds(.11f);
            blastCollider.enabled = false;
            yield return new WaitForSeconds(0.69f);
            Log($"Check index of enemy {markedEnemies.IndexOf(enemy)}");
            index = markedEnemies.IndexOf(enemy);
            Log("Index before clearing objects:" + index);
            if (index != -1)
			{
                if(markedEnemies[index]) Destroy(markedEnemies[index].GetComponent<Afflicted>());
                Destroy(_blast[index]);
                _blast.RemoveAt(index);
                Destroy(_focusLines[index]);
                _focusLines.RemoveAt(index);
                markedEnemies.RemoveAt(index);
            }

            Log("Blast Finished");
        }

        private void Log(object message) => Modding.Logger.Log("[FiveKnights][LamentControl] " + message);
    }

    internal class Afflicted : MonoBehaviour
    {
        public GameObject SoulEffect;
        private void Start()
        {
            SoulEffect = Instantiate(FiveKnights.preloadedGO["SoulEffect"], gameObject.transform);
            SoulEffect.transform.localPosition = new Vector3(0, 0, -0.0001f);
            Vector2 center = gameObject.transform.position;
            if (gameObject.GetComponent<SpriteRenderer>() != null) { center = gameObject.GetComponent<SpriteRenderer>().bounds.center; }
            if (gameObject.GetComponent<tk2dSprite>() != null) { center = gameObject.gameObject.GetComponent<tk2dSprite>().GetBounds().center + gameObject.transform.position; }
            SoulEffect.transform.position = center;
            SoulEffect.transform.localScale = new Vector3(.75f, .75f, .75f);

            SoulEffect.GetComponent<ParticleSystem>().startSize = .7f;
            SoulEffect.GetComponent<ParticleSystem>().startLifetime = .3f;
            SoulEffect.GetComponent<ParticleSystem>().startColor = new Color(1, 1, 1, .5f);

            SoulEffect.SetActive(true);
        }
        private void Update()
        {
            Vector3 center = gameObject.transform.position;
            if (gameObject.GetComponent<SpriteRenderer>() != null) { center = gameObject.GetComponent<SpriteRenderer>().bounds.center; }
            if (gameObject.GetComponent<tk2dSprite>() != null) { center = gameObject.GetComponent<tk2dSprite>().GetBounds().center + gameObject.transform.position; }
            SoulEffect.transform.position = new Vector3(center.x, center.y, gameObject.transform.position.z + -0.0001f);
            if (gameObject.GetComponent<SpriteRenderer>() != null) { SoulEffect.SetActive(gameObject.GetComponent<SpriteRenderer>().isVisible);}
            if (gameObject.GetComponent<MeshRenderer>() != null) { SoulEffect.SetActive(gameObject.GetComponent<MeshRenderer>().isVisible); }
        }
        public IEnumerator FadeOut()
        {
            //while (SoulEffect.GetComponent<SpriteRenderer>().color.a > 0)
            //{
            //   yield return new WaitForSeconds(.01f);
            //   SoulEffect.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, SoulEffect.GetComponent<SpriteRenderer>().color.a - .1f);
            // }
            SoulEffect.GetComponent<ParticleSystem>().Stop();
            yield return new WaitUntil(() => SoulEffect.GetComponent<ParticleSystem>().isStopped);
            SoulEffect.SetActive(false);
        }
        private void OnDisable()
        {
            Destroy(SoulEffect);
        }
        private void Log(object message) => Modding.Logger.Log("[FiveKnights][LamentControl] " + message);
    }

}
