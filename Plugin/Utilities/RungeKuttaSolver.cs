// Copyright (c) 2014 Morten Bakkedal
// This code is published under the MIT License.

using System;
using System.Collections.Generic;

namespace FuncLib.Mathematics.DifferentialEquations
{
	/// <summary>
	/// Provides several higher order implementations of Runge-Kutta initial value problem solvers. Fixed step
	/// strategies as well as adaptive strategies are implemented, all of them supporting both forward and backward
	/// integration through time.
	/// </summary>
	public static class RungeKuttaSolver
	{
		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$ using
		/// one fourth order Runge-Kutta step. The value $y(s)$ of $y$ at $t=s$ is returned.
		/// </summary>
		public static Vector Solve(Func<double, Vector, Vector> f, Vector y0, double s0, double s)
		{
			return Solve(f, y0, s0, s, 1);
		}

		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$ using
		/// a fixed number of fourth order Runge-Kutta steps. The value $y(s)$ of $y$ at $t=s$ is returned.
		/// </summary>
		public static Vector Solve(Func<double, Vector, Vector> f, Vector y0, double s0, double s, int n)
		{
			if (f == null || y0 == null)
			{
				throw new ArgumentNullException();
			}

			if (s == s0 && n >= 0)
			{
				// Avoid function evaluations in this special case.
				n = 0;
			}
			else if (n < 1)
			{
				throw new ArgumentException("Invalid number of steps.");
			}

			double h = (s - s0) / n;

			Vector y = y0;
			for (int i = 0; i < n; i++)
			{
				double t = s0 + i * h;

				Vector k1 = f(t, y);
				Vector k2 = f(t + 0.5 * h, y + 0.5 * h * k1);
				Vector k3 = f(t + 0.5 * h, y + 0.5 * h * k2);
				Vector k4 = f(t + h, y + h * k3);

				y += h / 6.0 * (k1 + 2.0 * k2 + 2.0 * k3 + k4);
			}

			return y;
		}

		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$ using
		/// one fourth order Runge-Kutta-Cash–Karp step. The value $y(s)$ of $y$ at $t=s$ is returned.
		/// </summary>
		public static Vector SolveCashKarp(Func<double, Vector, Vector> f, Vector y0, double s0, double s)
		{
			// Errors are estimated (requiring two additional function evaluations), but not used.
			Vector yerr;
			return SolveCashKarp(f, y0, s0, s, out yerr);
		}

		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$ using
		/// one fourth order Runge-Kutta-Cash–Karp step. The value $y(s)$ of $y$ at $t=s$ is returned. Absolute errors
		/// are estimated using an embedded fifth order Runge-Kutta step.
		/// </summary>
		public static Vector SolveCashKarp(Func<double, Vector, Vector> f, Vector y0, double s0, double s, out Vector yerr)
		{
			if (f == null || y0 == null)
			{
				throw new ArgumentNullException();
			}

			return SolveCashKarp(f, y0, s0, s, f(s0, y0), out yerr);
		}

		private static Vector SolveCashKarp(Func<double, Vector, Vector> f, Vector y0, double s0, double s, Vector dy0, out Vector yerr)
		{
			double h = s - s0;

			// First step.
			Vector k1 = dy0;
			Vector y1 = y0 + h * 0.2 * k1;

			// Second step.
			Vector k2 = f(s0 + 0.2 * h, y1);
			Vector y2 = y0 + h * (0.075 * k1 + 0.225 * k2);

			// Third step.
			Vector k3 = f(s0 + 0.3 * h, y2);
			Vector y3 = y0 + h * (0.3 * k1 - 0.9 * k2 + 1.2 * k3);

			// Fourth step.
			Vector k4 = f(s0 + 0.6 * h, y3);
			Vector y4 = y0 + h * (-0.20370370370370369 * k1 + 2.5 * k2 - 2.5925925925925926 * k3 + 1.2962962962962963 * k4);

			// Fifth step.
			Vector k5 = f(s0 + h, y4);
			Vector y5 = y0 + h * (0.029495804398148147 * k1 + 0.341796875 * k2 + 0.041594328703703706 * k3 + 0.40034541377314814 * k4 + 0.061767578125 * k5);

			// Sixth step.
			Vector k6 = f(s0 + 0.875 * h, y5);

			// Accumulate increments with proper weights.
			Vector y = y0 + h * (0.097883597883597878 * k1 + 0.40257648953301128 * k3 + 0.21043771043771045 * k4 + 0.28910220214568039 * k6);

			// Estimate error as difference between fourth and fifth order methods.
			yerr = h * (-0.0042937748015873106 * k1 + 0.018668586093857853 * k3 - 0.034155026830808066 * k4 - 0.019321986607142856 * k5 + 0.039102202145680387 * k6);

			return y;
		}

		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$ using
		/// fourth order Runge-Kutta-Cash–Karp steps as specified, thus reproducing the value computed by
		/// <see cref="SolveCashKarpAdaptive" />. The first step must be equal to $s_0$ and the last step must be the
		/// step just prior to $s$. The value $y(s)$ of $y$ at $t=s$ is returned.
		/// </summary>
		public static Vector SolveCashKarp(Func<double, Vector, Vector> f, Vector y0, double s0, double s, double[] t)
		{
			return Solve(f, y0, s0, s, t, SolveCashKarp);
		}

		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$ using
		/// the adaptive fourth order Runge-Kutta-Cash–Karp. The value $y(s)$ of $y$ at $t=s$ is returned.
		/// </summary>
		public static Vector SolveCashKarpAdaptive(Func<double, Vector, Vector> f, Vector y0, double s0, double s, double eps, double h0, double hmin)
		{
			return SolveAdaptive(f, y0, s0, s, eps, h0, hmin, SolveCashKarp);
		}

		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$ using
		/// the adaptive fourth order Runge-Kutta-Cash–Karp. The value $y(s)$ of $y$ at $t=s$ is returned. All
		/// intermediate steps are stored.
		/// </summary>
		public static Vector SolveCashKarpAdaptive(Func<double, Vector, Vector> f, Vector y0, double s0, double s, double eps, double h0, double hmin, out double[] t)
		{
			return SolveAdaptive(f, y0, s0, s, eps, h0, hmin, SolveCashKarp, out t);
		}

		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$ using
		/// one fifth order Runge-Kutta-Verner step. The value $y(s)$ of $y$ at $t=s$ is returned.
		/// </summary>
		public static Vector SolveVerner(Func<double, Vector, Vector> f, Vector y0, double s0, double s)
		{
			// Errors are estimated (requiring two additional function evaluations), but not used.
			Vector yerr;
			return SolveVerner(f, y0, s0, s, out yerr);
		}

		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$ using
		/// one fifth order Runge-Kutta-Verner step. The value $y(s)$ of $y$ at $t=s$ is returned. Absolute errors are
		/// estimated using an embedded sixth order Runge-Kutta step.
		/// </summary>
		public static Vector SolveVerner(Func<double, Vector, Vector> f, Vector y0, double s0, double s, out Vector yerr)
		{
			if (f == null || y0 == null)
			{
				throw new ArgumentNullException();
			}

			return SolveVerner(f, y0, s0, s, f(s0, y0), out yerr);
		}

		private static Vector SolveVerner(Func<double, Vector, Vector> f, Vector y0, double s0, double s, Vector dy0, out Vector yerr)
		{
			double h = s - s0;

			Vector k1 = dy0;
			Vector y1 = y0 + h * 0.055555555555555552 * k1;

			Vector k2 = f(s0 + 0.055555555555555552 * h, y1);
			Vector y2 = y0 + h * (-0.083333333333333329 * k1 + 0.25 * k2);

			Vector k3 = f(s0 + 0.16666666666666666 * h, y2);
			Vector y3 = y0 + h * (-0.024691358024691357 * k1 + 0.14814814814814814 * k2 + 0.098765432098765427 * k3);

			Vector k4 = f(s0 + 0.22222222222222221 * h, y3);
			Vector y4 = y0 + h * (1.2121212121212122 * k1 - 0.36363636363636365 * k2 - 5.0909090909090908 * k3 + 4.9090909090909092 * k4);

			Vector k5 = f(s0 + 0.66666666666666663 * h, y4);
			Vector y5 = y0 + h * (-5.0547945205479454 * k1 + 0.98630136986301364 * k2 + 24.5662100456621 * k3 - 21.035958904109588 * k4 + 1.53824200913242 * k5);

			Vector k6 = f(s0 + h, y5);
			Vector y6 = y0 + h * (-9.78226711560045 * k1 + 2.2087542087542089 * k2 + 44.354657687991022 * k3 - 37.81818181818182 * k4 + 1.9259259259259258 * k5);

			Vector k7 = f(s0 + 0.88888888888888884 * h, y6);
			Vector y7 = y0 + h * (11.77734375 * k1 - 2.25 * k2 - 54.089743589743591 * k3 + 46.7578125 * k4 - 1.4036458333333333 * k5 + 0.20823317307692307 * k7);

			Vector k8 = f(s0 + h, y7);

			Vector y = y0 + h * (0.0375 * k1 + 0.16 * k3 + 0.21696428571428572 * k4 + 0.48125 * k5 + 0.10428571428571429 * k6);
			yerr = h * (0.0515625 * k1 - 0.40615384615384614 * k3 + 0.39776785714285712 * k4 - 0.103125 * k5 - 0.10428571428571429 * k6 + 0.10709134615384615 * k7 + 0.057142857142857141 * k8);

			return y;
		}

		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$ using
		/// fifth order Runge-Kutta-Verner steps as/ specified, thus reproducing the value computed by
		/// <see cref="SolveVernerAdaptive" />. The first step must be equal to $s_0$ and the last step must be the
		/// step just prior to $s$. The value $y(s)$ of $y$ at $t=s$ is returned.
		/// </summary>
		public static Vector SolveVerner(Func<double, Vector, Vector> f, Vector y0, double s0, double s, double[] t)
		{
			return Solve(f, y0, s0, s, t, SolveVerner);
		}

		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$ using
		/// the adaptive fifth order Runge-Kutta-Verner. The value $y(s)$ of $y$ at $t=s$ is returned.
		/// </summary>
		public static Vector SolveVernerAdaptive(Func<double, Vector, Vector> f, Vector y0, double s0, double s, double eps, double h0, double hmin)
		{
			return SolveAdaptive(f, y0, s0, s, eps, h0, hmin, SolveVerner);
		}

		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$ using
		/// the adaptive fifth order Runge-Kutta-Verner. The value $y(s)$ of $y$ at $t=s$ is returned. All intermediate
		/// steps are stored.
		/// </summary>
		public static Vector SolveVernerAdaptive(Func<double, Vector, Vector> f, Vector y0, double s0, double s, double eps, double h0, double hmin, out double[] t)
		{
			return SolveAdaptive(f, y0, s0, s, eps, h0, hmin, SolveVerner, out t);
		}

		/// <summary>
		/// Stepper used for Runge-Kutta.
		/// </summary>
		public delegate Vector Stepper(Func<double, Vector, Vector> f, Vector y0, double s0, double s);

		/// <summary>
		/// Stepper used for adaptive Runge-Kutta.
		/// </summary>
		public delegate Vector StepperAdaptive(Func<double, Vector, Vector> f, Vector y0, double s0, double s, Vector dy0, out Vector yerr);

		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$. Used by
		/// <see cref="SolveCashKarp" /> and <see cref="SolveVerner" />.
		/// </summary>
		public static Vector Solve(Func<double, Vector, Vector> f, Vector y0, double s0, double s, double[] t, Stepper stepper)
		{
			if (f == null || y0 == null || t == null || stepper == null)
			{
				throw new ArgumentNullException();
			}

			int n = t.Length;
			Vector y = y0;

			if (n != 0 && t[0] != s0 || n == 0 && s0 != s)
			{
				throw new ArgumentException("Invalid first step.");
			}

			for (int i = 0; i < n; i++)
			{
				double u0 = t[i];
				double u = i + 1 < n ? t[i + 1] : s;

				// Don't allow step direction to reverse or to overshoot (but allow steps of size 0).
				if (s > s0 && !(s >= u && u >= u0) || s < s0 && !(s <= u && u <= u0))
				{
					throw new ArgumentException("Invalid step.");
				}

				if (u != u0)
				{
					y = stepper(f, y, u0, u);
				}
			}

			return y;
		}

		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$. Used by
		/// <see cref="SolveCashKarpAdaptive" /> and <see cref="SolveVernerAdaptive" />.
		/// </summary>
		public static Vector SolveAdaptive(Func<double, Vector, Vector> f, Vector y0, double s0, double s, double eps, double h0, double hmin, StepperAdaptive stepper)
		{
			return SolveAdaptive(f, y0, s0, s, eps, h0, hmin, stepper, null);
		}

		/// <summary>
		/// Solves the differential equation $y'=f(t,y)$ from $t=s_0$ to $t=s$ with initial value $y(s_0)=y_0$. Used by
		/// <see cref="SolveCashKarpAdaptive" /> and <see cref="SolveVernerAdaptive" />.
		/// </summary>
		public static Vector SolveAdaptive(Func<double, Vector, Vector> f, Vector y0, double s0, double s, double eps, double h0, double hmin, StepperAdaptive stepper, out double[] t)
		{
			List<double> tsteps = new List<double>();
			Vector y = SolveAdaptive(f, y0, s0, s, eps, h0, hmin, stepper, tsteps);
			t = tsteps.ToArray();
			return y;
		}

		private static Vector SolveAdaptive(Func<double, Vector, Vector> f, Vector y0, double s0, double s, double eps, double h0, double hmin, StepperAdaptive stepper, List<double> tsteps)
		{
			if (f == null || y0 == null || stepper == null)
			{
				throw new ArgumentNullException();
			}

			if (eps <= 0.0)
			{
				throw new ArgumentOutOfRangeException("Invalid target accuracy.");
			}

			if (hmin < 0.0 || h0 < hmin || h0 == 0.0)
			{
				throw new ArgumentOutOfRangeException("Invalid step size specification.");
			}

			// Use initial value to determine the dimension of the problem.
			int n = y0.Length;

			//  Use this step size for the first step (unless determined too large).
			double h = h0;

			// Negative step size if integrating backwards through time.
			if (s < s0)
			{
				h = -h;
				hmin = -hmin;
			}

			double t = s0;
			Vector y = y0;

			while (t != s)
			{
				// Store intermediate steps if allocated.
				if (tsteps != null)
				{
					tsteps.Add(t);
				}

				// Try this step. And maintain a minimum step size.
				double u = t + h;
				double umin = t + hmin;

				// Don't allow the current step to overshoot.
				if (s > s0 && u > s || s < s0 && u < s)
				{
					u = s;
				}

				// May take a smaller step than the minimum step size at the very last step to avoid overshooting.
				if (s > s0 && umin > s || s < s0 && umin < s)
				{
					umin = s;
				}

				// Evaluate derivative at the initial point for this step. This may be reused even if guessed step size is too large.
				Vector dy = f(t, y);
				Vector z;

				while (true)
				{
					// Don't go below the minimum step size.
					if (s > s0 && u < umin || s < s0 && u > umin)
					{
						u = umin;
						h = hmin;
					}

					if (u == t)
					{
						throw new ArithmeticException("Adaptive step size underflow.");
					}

					// Take a step.
					Vector yerr;
					z = stepper(f, y, t, u, dy, out yerr);

					// Evaluate accuracy. And scale relative to required tolerance.
					double errmax = 0.0;
					for (int i = 0; i < n; i++)
					{
						// Scaling to monitor accuracy. This is a general-purpose choice.
						double yscal = Math.Abs(y[i]) + Math.Abs(dy[i] * h);

						// Need this to be non-zero. Otherwise the entry can't be that important.
						if (yscal != 0.0)
						{
							errmax = Math.Max(errmax, Math.Abs(yerr[i] / yscal));
						}
					}
					errmax /= eps;

					if (errmax <= 1.0)
					{
						// Step succeeded. Increase the step size for the next step, but no more than a factor of 5.
						h *= Math.Min(5.0, 0.9 * Math.Pow(errmax, -0.2));
						break;
					}

					if (u == umin)
					{
						// Already at minimum step size. Not allowed to improve the step size below this.
						break;
					}

					// Not succeeded: Truncation error too large, reduce stepsize, but no more than a factor of 10.
					h *= Math.Max(0.1, 0.9 * Math.Pow(errmax, -0.25));
					u = t + h;

					// Restart loop with the reduced step size.
				}

				// Finish the step.
				t = u;
				y = z;
			}

			return y;
		}
	}
}
