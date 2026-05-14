import { USER_ROLE_LIST } from '../domain/roles';
import AppSelect from './AppSelect';

export default function RoleSelector({ value, onChange, className = '', disabled = false }) {
  return (
    <AppSelect
      value={value}
      onChange={onChange}
      className={className}
      disabled={disabled}
      options={USER_ROLE_LIST}
    />
  );
}
