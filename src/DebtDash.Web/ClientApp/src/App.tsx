import { BrowserRouter, Routes, Route, NavLink } from 'react-router-dom';
import LoanSetupPage from './pages/LoanSetupPage';
import LedgerPage from './pages/LedgerPage';
import DashboardPage from './pages/DashboardPage';
import PaymentCalculatorPage from './pages/PaymentCalculatorPage';
import './App.css';

function App() {
  return (
    <BrowserRouter>
      <div className="app">
        <nav className="nav" aria-label="Main navigation">
          <h2 className="nav-brand">DebtDash</h2>
          <ul className="nav-links">
            <li><NavLink to="/">Loan Setup</NavLink></li>
            <li><NavLink to="/ledger">Payment Ledger</NavLink></li>
            <li><NavLink to="/dashboard">Dashboard</NavLink></li>
            <li><NavLink to="/calculator">Payment Calculator</NavLink></li>
          </ul>
        </nav>
        <div className="content">
          <Routes>
            <Route path="/" element={<LoanSetupPage />} />
            <Route path="/ledger" element={<LedgerPage />} />
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/calculator" element={<PaymentCalculatorPage />} />
          </Routes>
        </div>
      </div>
    </BrowserRouter>
  );
}

export default App;
