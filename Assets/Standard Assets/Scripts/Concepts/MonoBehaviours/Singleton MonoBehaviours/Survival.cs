using TMPro;
using System;
using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace SlimeJump
{
	public class Survival : SingletonMonoBehaviour<Survival>
	{
		public TMP_Text waveText;
		public TMP_Text bestWaveText;
		public uint difficulty;
		public uint addToDifficulty;
		public EnemyEntry[] enemyEntries = new EnemyEntry[0];
		public Splat enemySpawnSplatPrefab;
		public GameObject[] areaGos = new GameObject[0];
		public int BestWave
		{
			get
			{
				return SaveAndLoadManager.GetInt("Best wave", 1);
			}
			set
			{
				SaveAndLoadManager.SetInt ("Best wave", value);
			}
		}
		uint enemiesRemaining;
		uint wave;

		void Start ()
		{
			Lasso.instance.gameObject.SetActive(true);
			Blaster.Instance.gameObject.SetActive(true);
			areaGos[Random.Range(0, areaGos.Length)].SetActive(true);
			bestWaveText.text = "Best wave: " + BestWave;
			NextWave ();
		}

		void NextWave ()
		{
			Player.instance.TakeDamage (-1, default(Vector2), null);
			SpawnEnemies ();
			difficulty += addToDifficulty;
			wave ++;
			if (wave > BestWave)
				BestWave = (int) wave;
			waveText.text = "Wave " + wave;
		}

		void SpawnEnemies ()
		{
			List<EnemyEntry> remainingEnemyEntries = new List<EnemyEntry>(enemyEntries);
			uint remainingDifficulty = difficulty;
			while (remainingEnemyEntries.Count > 0)
			{
				int randomIndex = Random.Range(0, remainingEnemyEntries.Count);
				EnemyEntry enemyEntry = enemyEntries[randomIndex];
				if (enemyEntry.difficulty <= remainingDifficulty)
				{
					remainingDifficulty -= enemyEntry.difficulty;
					Vector2 spawnPos = enemyEntry.spawnPoints[Random.Range(0, enemyEntry.spawnPoints.Length)].position;
					Splat enemySpawnSplat = Instantiate(enemySpawnSplatPrefab, spawnPos, Quaternion.identity);
					enemySpawnSplat.onDestroy.AddListener(() => { enemyEntry.Spawn (spawnPos); });
					enemiesRemaining ++;
				}
				else
					remainingEnemyEntries.RemoveAt(randomIndex);
			}
		}

		void OnEnemyDied (Enemy enemy)
		{
			enemiesRemaining --;
			if (enemiesRemaining == 0)
				NextWave ();
		}

		public void OnPlayerDied ()
		{
			Splat[] splats = FindObjectsOfType<Splat>();
			for (int i = 0; i < splats.Length; i ++)
			{
				Splat splat = splats[i];
				Destroy(splat.gameObject);
			}
			for (int i = 0; i < Enemy.instances.Length; i ++)
			{
				Enemy enemy = Enemy.instances[i];
				Destroy(enemy.gameObject);
				i --;
			}
			difficulty -= addToDifficulty * wave;
			wave = 0;
			enemiesRemaining = 0;
			Start ();
		}

		[Serializable]
		public class EnemyEntry
		{
			public Enemy enemyPrefab;
			public uint difficulty;
			public Transform[] spawnPoints = new Transform[0];

			public Enemy Spawn (Vector2 pos)
			{
				Enemy enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
				enemy.onDied.AddListener((Enemy enemy) => { instance.OnEnemyDied (enemy); });
				enemy.ChasePlayer ();
				return enemy;
			}
		}
	}
}