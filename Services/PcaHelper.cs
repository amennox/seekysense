using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

public static class PcaHelper
{
    /// Calcola il primo autovettore (componente principale) di una matrice embedding Nx1024.
    /// <param name="data">Matrice double[N,1024] (es. 20x1024)</param>
    /// <returns>Vettore double[1024] - prima componente principale normalizzata</returns>
    public static double[] CalcolaAutovettorePrincipale(double[,] data)
    {
        int n = data.GetLength(0);
        int d = data.GetLength(1);

        if (d != 1024)
            throw new ArgumentException("La matrice deve avere 1024 colonne.");
        if (n < 2)
            throw new ArgumentException("Servono almeno 2 vettori per calcolare la PCA.");

        // Crea matrice MathNet
        var matrix = DenseMatrix.OfArray(data);

        // Centrare i dati (sottraiamo la media da ogni colonna)
        var meanVector = matrix.ColumnSums() / n;
        for (int i = 0; i < n; i++)
        {
            matrix.SetRow(i, matrix.Row(i) - meanVector);
        }

        // Calcola matrice di covarianza (d x d)
        var covarianceMatrix = (matrix.TransposeThisAndMultiply(matrix)) / (n - 1);

        // Decomposizione agli autovalori
        var evd = covarianceMatrix.Evd(Symmetricity.Symmetric);

        // Trova indice dell'autovalore massimo
        int maxIndex = 0;
        double maxEigenvalue = evd.EigenValues[0].Real;
        for (int i = 1; i < d; i++)
        {
            double val = evd.EigenValues[i].Real;
            if (val > maxEigenvalue)
            {
                maxEigenvalue = val;
                maxIndex = i;
            }
        }

        // Estrai e normalizza l'autovettore corrispondente
        var principalComponent = evd.EigenVectors.Column(maxIndex);
        principalComponent = principalComponent.Normalize(2); // Normalizza L2

        return principalComponent.ToArray();
    }
}
