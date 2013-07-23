using UnityEditor;
using UnityEngine;

using sqrt = Mathf.Sqrt
using sqrt = Mathf.Sqrt


//Spherical Harmonic Class


public class Atom_SH : MonoBehaviour {


  public static int SH_Factorial( int n )
	{
		int result = 1;

		for( int i=2; i<=n; ++i )
			result *= i;

		return result;
	}

	/// Evaluates the Associated Legendre Polynomial with parameters (l,m) at x.
	public static double SH_P( int l, int m, double x )
	{
		// Code taken from 'Robin Green - Spherical Harmonic Lighting'.
		double pmm = 1;
		if( m > 0 )
		{
			double somx2 = sqrt( (1-x)*(1+x) );
			double fact = 1;
			for( int i=1; i<=m; ++i )
			{
				pmm *= -fact * somx2;
				fact += 2;
			}
		}

		if( l == m )
			return pmm;

		double pmmp1 = x * (2*m+1) * pmm;
		if( l == m+1 )
			return pmmp1;

		double pll = 0.0;
		for( int ll=m+2; ll<=l; ++ll )
		{
			pll = ( (2*ll-1) * x * pmmp1 - (ll+m-1) * pmm ) / (ll-m);
			pmm = pmmp1;
			pmmp1 = pll;
		}

		return pll;
	}

	/// Returns the normalization constant for the SH basis function with parameters (l,m).
	public static double SH_K( int l, int m )
	{
		return sqrt( ( (2*l+1) * SH_Factorial(l-m) ) / ( 4*PI * SH_Factorial(l+m) ) );
	}

	/// Evaluates the real SH basis function with parameters (l,m) at (theta,phi).
	public static double SH_Y( int l, int m, double theta, double phi )
	{
		const double sqrt2 = sqrt( static_cast< double >(2.0) );
		if( m == 0 )
			return K(l,0) * P(l,m,cos(theta));
		else if( m > 0 )
			return sqrt2 * K(l,m) * cos(m*phi) * P(l,m,cos(theta));
		else
			return sqrt2 * K(l,-m) * sin(-m*phi) * P(l,-m,cos(theta));
	}

}
