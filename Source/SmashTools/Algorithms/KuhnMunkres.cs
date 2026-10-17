using System;
using SmashTools.Performance;
using UnityEngine;

namespace SmashTools.Algorithms;

/// <summary>
/// <see href="https://en.wikipedia.org/wiki/Hungarian_algorithm"/>
/// </summary>
public sealed class KuhnMunkres
{
	// Square matrix dimension
	private readonly int n;

	private readonly float[] labelByWorker;
	private readonly float[] labelByJob;
	private readonly int[] minSlackWorkerByJob;
	private readonly float[] minSlackValueByJob;
	private readonly int[] matchWorkerByJob;
	private readonly int[] parentWorkerByCommittedJob;
	private readonly bool[] committedWorkers;

	/// <summary>
	/// Initialize a KuhnMunkres matrix solver, distributing <paramref name="n"/> workers to <paramref name="n"/> potential jobs
	/// </summary>
	/// <param name="n">Number of workers to distribute.</param>
	public KuhnMunkres(int n)
	{
		this.n = n;

		labelByWorker = new float[n];
		labelByJob = new float[n];
		minSlackWorkerByJob = new int[n];
		minSlackValueByJob = new float[n];
		matchWorkerByJob = new int[n];
		parentWorkerByCommittedJob = new int[n];
		committedWorkers = new bool[n];
	}

	[Profile]
	public int[] Compute(float[,] costMatrix)
	{
		if (costMatrix.GetLength(0) != n || costMatrix.GetLength(1) != n)
			throw new ArgumentException("costMatrix must be NxN matrix.");

		int[] result = new int[n];
		result.Populate(-1);
		Array.Clear(labelByJob, 0, labelByJob.Length);
		matchWorkerByJob.Populate(-1);

		InitializeLabels(costMatrix);

		for (int i = 0; i < n; i++)
		{
			// Only execute on indices that still need processing
			if (result[i] >= 0)
				continue;

			StartPhase(i, costMatrix);
			ExecutePhase(costMatrix, result);
		}
		return result;
	}

	private void InitializeLabels(float[,] costMatrix)
	{
		for (int i = 0; i < n; i++)
		{
			labelByWorker[i] = float.MaxValue;
			for (int j = 0; j < n; j++)
				labelByWorker[i] = Mathf.Min(labelByWorker[i], costMatrix[i, j]);
		}
	}

	private void StartPhase(int i, float[,] costMatrix)
	{
		committedWorkers.Populate(false);
		parentWorkerByCommittedJob.Populate(-1);
		committedWorkers[i] = true;

		for (int j = 0; j < n; j++)
		{
			minSlackValueByJob[j] = costMatrix[i, j] - labelByWorker[i] - labelByJob[j];
			minSlackWorkerByJob[j] = i;
		}
	}

	private void ExecutePhase(float[,] costMatrix, int[] result)
	{
		while (true)
		{
			float minSlackValue = float.MaxValue;
			int minSlackWorker = -1, minSlackJob = -1;

			for (int j = 0; j < n; j++)
			{
				if (parentWorkerByCommittedJob[j] == -1 && minSlackValueByJob[j] < minSlackValue)
				{
					minSlackValue = minSlackValueByJob[j];
					minSlackWorker = minSlackWorkerByJob[j];
					minSlackJob = j;
				}
			}

			for (int j = 0; j < n; j++)
			{
				if (committedWorkers[j])
					labelByWorker[j] += minSlackValue;
			}

			for (int j = 0; j < n; j++)
			{
				if (parentWorkerByCommittedJob[j] != -1)
					labelByJob[j] -= minSlackValue;
				else
					minSlackValueByJob[j] -= minSlackValue;
			}

			parentWorkerByCommittedJob[minSlackJob] = minSlackWorker;

			if (matchWorkerByJob[minSlackJob] == -1)
			{
				int parentWorker = parentWorkerByCommittedJob[minSlackJob];
				while (true)
				{
					int temp = result[parentWorker];
					result[parentWorker] = minSlackJob;
					matchWorkerByJob[minSlackJob] = parentWorker;
					minSlackJob = temp;
					if (minSlackJob == -1)
						break;
					parentWorker = parentWorkerByCommittedJob[minSlackJob];
				}
				break;
			}

			int worker = matchWorkerByJob[minSlackJob];
			committedWorkers[worker] = true;
			for (int j = 0; j < n; j++)
			{
				if (parentWorkerByCommittedJob[j] != -1)
					continue;

				float slack = costMatrix[worker, j] - labelByWorker[worker] - labelByJob[j];
				if (minSlackValueByJob[j] > slack)
				{
					minSlackValueByJob[j] = slack;
					minSlackWorkerByJob[j] = worker;
				}
			}
		}
	}
}